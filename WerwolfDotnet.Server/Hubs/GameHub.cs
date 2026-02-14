using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Hubs;

[Authorize]
public sealed class GameHub(ILogger<GameHub> logger, PlayerConnectionMapper connectionMapping, GameManager manager) : Hub<IGameHub>
{
    private readonly ILogger _logger = logger;
    private readonly PlayerConnectionMapper _connectionMapping = connectionMapping;
    private readonly GameManager _manager = manager;
    
    public override async Task OnConnectedAsync()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        Player player = ctx.Players.Single(p => p.Id == Context.User!.GetPlayerId());
        
        _connectionMapping.AddConnectionToPlayer(ctx.Id, player.Id, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Game(ctx.Id));

        await Clients.Caller.GameMetaUpdated(ctx.GameMaster.Id, ctx.Mayor?.Id);
        await Clients.Caller.GameStateUpdated(ctx.State, new Dictionary<int, DeathDetails>(0));
        await Clients.Caller.PlayersUpdated(ctx.Players.ToDtoCollection());

        if (player.Role is not null)
            await Clients.Caller.PlayerRoleUpdated(player.Role.Type);

        if (ctx.RunningAction is { } action && action.Participants.Contains(player))
        {
            await Clients.Caller.PlayerActionRequested(new SelectionOptionsDto(action, player));

            if (action.Participants.Count > 1)
            {
                IReadOnlyDictionary<int, int[]> votedPlayers = action.PlayerVotes
                    .Select(kvp => KeyValuePair.Create(kvp.Key.Id, kvp.Value.Select(p => p.Id).ToArray()))
                    .ToDictionary();
                await Clients.Caller.VotesUpdated(votedPlayers);
            }
        }
        
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        int gameId = Context.User!.GetGameId();
        int playerId = Context.User!.GetPlayerId();
        _connectionMapping.RemoveConnectionFromPlayer(gameId, playerId, Context.ConnectionId);
        
        return base.OnDisconnectedAsync(exception);
    }

    [HubMethodName("toggleGameLock")]
    public async Task OnToggleGameLocked()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (!CheckGameMaster(ctx, "toggle game lock"))
            return;

        await _manager.ToggleGameLockedAsync(ctx);
    }

    [HubMethodName("shufflePlayers")]
    public async Task OnShufflePlayers()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (!CheckGameMaster(ctx, "shuffle players"))
            return;

        await _manager.ShuffelPlayersAsync(ctx);
    }

    [HubMethodName("startGame")]
    public async Task OnStartGame()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (!CheckGameMaster(ctx, "start game"))
            return;
        
        await _manager.StartGameAsync(ctx);
    }

    [HubMethodName("playerAction")]
    public async Task OnPlayerAction(int[] selectedPlayers)
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        Player self = ctx.Players.Single(p => p.Id == Context.User!.GetPlayerId());

        Player[] votes = [..ctx.Players.Where(p => selectedPlayers.Contains(p.Id))];
        await _manager.RegisterPlayerActionAsync(ctx, self, votes);
    }
    
    [HubMethodName("leaveGame")]
    public async Task OnPlayerLeaving(int? playerId = null)
    {
        int selfId = Context.User!.GetPlayerId();
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (playerId is not null && ctx.GameMaster.Id != selfId)
        {
            _logger.LogWarning("Non-game-master {playerId} tried to kick player {playerToKick} (rejected)", selfId, playerId);
            return;
        }
        playerId ??= selfId;
        
        Player playerToKick = ctx.Players.Single(p => p.Id == playerId);

        string[] playerConnections = _connectionMapping.GetPlayerConnections(ctx.Id, playerToKick.Id);
        foreach (string connectionId in playerConnections)
            await Groups.RemoveFromGroupAsync(connectionId, GroupNames.Game(ctx.Id));
        
        await _manager.LeaveGameAsync(ctx, playerToKick);
        await Clients.Player(ctx.Id, playerToKick.Id).ForceDisconnect(kicked: playerId != selfId);     // Not done by manager because it can't differentiate between leaving and kicking
    }

    private bool CheckGameMaster(GameContext ctx, string action)
    {
        int selfId = Context.User!.GetPlayerId();
        if (ctx.GameMaster.Id == selfId)
            return true;
        
        _logger.LogWarning($"Non-game-master {{playerId}} tried to {action}.", selfId);
        return false;
    }
}