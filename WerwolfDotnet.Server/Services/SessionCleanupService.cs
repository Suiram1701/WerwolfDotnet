using System.Timers;
using Microsoft.Extensions.Options;
using WerwolfDotnet.Logging;
using WerwolfDotnet.Server.Options;
using Timer = System.Timers.Timer;

namespace WerwolfDotnet.Server.Services;

public sealed class SessionCleanupService(
    ILogger<SessionCleanupService> logger,
    GameManager manager,
    IOptions<CleanupOptions> optionsProvider) : IHostedService
{
    private readonly ILogger _logger = logger;
    private readonly GameManager _manager = manager;
    private readonly Timer _timer = new(optionsProvider.Value.Intervall);
    
    private CleanupOptions Options => optionsProvider.Value; 
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Elapsed += OnElapsed;
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Stop();
        _timer.Elapsed -= OnElapsed;
        return Task.CompletedTask;
    }

    private async void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        _logger.LogTrace("Cleaning up sessions/players");
        try
        {
            IEnumerable<GameContext> contexts = (await _manager.GetAllGamesAsync(skipAccessCheck: true))!;
            foreach (GameContext ctx in contexts.ToArray())
            {
                if (Options.AfterInactivity < DateTimeOffset.UtcNow - ctx.Logger.Messages[^1].Time)
                    await removeSession(ctx);
                if (ctx.Players.Count == 1 && Options.AfterEmptySession < DateTimeOffset.UtcNow - (ctx.Logger.Messages
                        .LastOrDefault(msg => msg.Event == Event.Left)?.Time ?? ctx.Logger.Messages[^1].Time))
                    await removeSession(ctx);

                if (ctx.RoundStartedAt == DateTimeOffset.MinValue)     // No need to check
                    continue;
                foreach (Player player in ctx.Players.Where(p => p.Status == PlayerState.Alive).ToArray())     // Death players can't interact -> do not kick them
                {
                    (int index, LogMessage message)[] playerVotes = ctx.Logger.Messages
                        .Index() // The last n votings the player participated in
                        .Where(t => t.Item.Time >= ctx.RoundStartedAt)
                        .Where(t => t.Item.Event == Event.Voting && t.Item.Args.Skip(1)
                            .Any(kvp => ((KeyValuePair<LogPlayerSnapshot, LogPlayerSnapshot[]>)kvp!).Key.Equals(player)))
                        .Reverse()
                        .Take(Options.KickPlayerAfterInactiveActions)
                        .ToArray();
                    if (playerVotes.Length == Options.KickPlayerAfterInactiveActions && playerVotes.All(t =>     // player is kicked when he was in the last n votings absent
                        {
                            LogMessage previousMsg = ctx.Logger.Messages[t.index - 1];     // In case the voting timeouted, the corresponding message comes before
                            if (previousMsg.Event != Event.VotingTimeout)
                                return false;
                            return previousMsg.Args.Skip(1).Any(p => player.Equals(p));
                        }))
                    {
                        _logger.LogInformation("Removed player {player} from session {sessionId:D6} due to inactivity", player, ctx.Id);
                        await _manager.LeaveGameAsync(ctx, player, kicked: true); 
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to clean up sessions");
        }

        async Task removeSession(GameContext ctx)
        {
            _logger.LogInformation("Removed inactive session {sessionId:D6} with game master {gameMaster}", ctx.Id, ctx.GameMaster);
            foreach (Player player in ctx.Players.OrderBy(p => ctx.GameMaster.Equals(p)).ToArray())     // Order to prevent the UI from showing GM switch messages
                await _manager.LeaveGameAsync(ctx, player, kicked: true);     // When the whole session is removed connection groups doesn't really matter
        }
    }
}