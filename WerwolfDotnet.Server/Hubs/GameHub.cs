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
        foreach (string groupName in GetPlayerGroups(ctx, player))
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
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
        await _manager.LeaveGameAsync(ctx, playerToKick);

        await Clients.Group(GroupNames.Player(ctx.Id, playerId.Value)).ForceDisconnect(kicked: playerId != selfId);
        string[] playerConnections = _connectionMapping.GetPlayerConnections(ctx.Id, playerToKick.Id);
        foreach (string groupName in GetPlayerGroups(ctx, playerToKick))
        {
            foreach (string connectionId in playerConnections)
                await Groups.RemoveFromGroupAsync(connectionId, groupName);
        }
    }
    
    private static IEnumerable<string> GetPlayerGroups(GameContext ctx, Player player)
    {
        yield return GroupNames.Game(ctx.Id);
        yield return GroupNames.Player(ctx.Id, player.Id);
    }
}