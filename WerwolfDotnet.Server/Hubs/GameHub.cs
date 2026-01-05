using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Hubs;

[Authorize]
public sealed class GameHub(ILogger<GameHub> logger, GameToHubInterface connectionMapping, GameManager manager) : Hub<IGameHub>
{
    private readonly ILogger _logger = logger;
    private readonly GameToHubInterface _connectionMapping = connectionMapping;
    private readonly GameManager _manager = manager;
    
    public override async Task OnConnectedAsync()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        Player player = ctx.Players.Single(p => p.Id == Context.User!.GetPlayerId());
        
        _connectionMapping.AddConnectionToPlayer(ctx.Id, player.Id, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Game(ctx.Id));

        await Clients.Caller.GameMetaUpdated(new GameMetadataDto
        {
            GameMasterId = ctx.GameMaster.Id,
            MayorId = ctx.Mayor?.Id,
        });
        await Clients.Caller.GameStateUpdated(new GameStateDto { CurrentState = ctx.State });
        await Clients.Caller.PlayersUpdated(ctx.Players.Select(p => new PlayerDto(p)));
        
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
        int selfId = Context.User!.GetPlayerId();
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (ctx.GameMaster.Id != selfId)
        {
            _logger.LogWarning("Non-game-master {playerId} tried to toggle the lock-mode.", selfId);
            return;
        }

        await _manager.ToggleGameLockedAsync(ctx);
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
        await Clients.Player(ctx.Id, playerToKick.Id).ForceDisconnect(kicked: playerId != selfId);
        
        await _manager.LeaveGameAsync(ctx, playerToKick);
    }
}