using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Hubs;

[Authorize]
public sealed class GameHub(GameManager manager) : Hub<IGameHub>
{
    private readonly GameManager _manager = manager;
    
    public override async Task OnConnectedAsync()
    {
        int gameId = Context.User!.GetGameId();
        GameContext ctx = (await _manager.GetGameById(gameId))!;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Game(gameId));
        await Clients.Caller.PlayersUpdated(ctx.Players.Select(p => new PlayerDto(p)));
        await base.OnConnectedAsync();
    }
    
    [HubMethodName("leaveGame")]
    public async Task OnLeaveGameAsync()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        Player player = ctx.Players.Single(p => p.Id == Context.User!.GetGameId());
        await _manager.LeaveGameAsync(ctx, player);
        
        Context.Abort();
    }
}