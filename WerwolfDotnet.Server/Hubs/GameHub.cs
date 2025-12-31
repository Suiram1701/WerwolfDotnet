using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Hubs;

[Authorize]
public sealed class GameHub(ILogger<GameHub> logger, GameManager manager) : Hub<IGameHub>
{
    private readonly ILogger _logger = logger;
    private readonly GameManager _manager = manager;
    
    public override async Task OnConnectedAsync()
    {
        int gameId = Context.User!.GetGameId();
        GameContext ctx = (await _manager.GetGameById(gameId))!;
        Player player = ctx.Players.Single(p => p.Id == Context.User!.GetPlayerId());
        
        foreach (string groupName in GetPlayerGroups(ctx, player))
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.PlayersUpdated(ctx.Players.Select(p => new PlayerDto(p)));
        
        await base.OnConnectedAsync();
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
        foreach (string groupName in GetPlayerGroups(ctx, playerToKick))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
    
    private static IEnumerable<string> GetPlayerGroups(GameContext ctx, Player player)
    {
        yield return GroupNames.Game(ctx.Id);
        yield return GroupNames.Player(ctx.Id, player.Id);
    }
}