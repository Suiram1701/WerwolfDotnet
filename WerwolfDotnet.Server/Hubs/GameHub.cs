using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WerwolfDotnet.Server.Hubs;

[Authorize]
public sealed class GameHub : Hub<IGameHub>
{
    public override async Task OnConnectedAsync()
    {
        int gameId = Context.User!.GetGameId();
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Game(gameId));
        
        await base.OnConnectedAsync();
    }
}