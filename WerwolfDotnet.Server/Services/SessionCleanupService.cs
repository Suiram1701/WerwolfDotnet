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
        try
        {
            IEnumerable<GameContext> contexts = (await _manager.GetAllGames(skipAccessCheck: true))!;
            foreach (GameContext ctx in contexts.ToArray())
            {
                if (Options.AfterInactivity < DateTimeOffset.UtcNow - ctx.Logger.Messages[^1].Time)
                    await removeSession(ctx);
                if (ctx.Players.Count == 1 && Options.AfterEmptySession < DateTimeOffset.UtcNow - (ctx.Logger.Messages
                        .LastOrDefault(msg => msg.Event == Event.Left)?.Time ?? ctx.Logger.Messages[^1].Time))
                    await removeSession(ctx);
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