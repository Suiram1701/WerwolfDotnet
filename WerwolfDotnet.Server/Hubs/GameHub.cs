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
        await Clients.Caller.GameStateUpdated(ctx.State, new Dictionary<int, DeathDetails>(0), null);
        await _manager.UpdatePlayersAsync(ctx, player);
        await _manager.UpdatePlayerRoleAsync(ctx, player);

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

    [HubMethodName("setGameLocked")]
    public async Task OnSetGameLocked(bool locked)
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (!CheckGameMaster(ctx, "toggle game lock"))
            return;

        await _manager.SetGameLockedAsync(ctx, locked);
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

    [HubMethodName("cancelCurrentPlayerAction")]
    public async Task OnCancelCurrentPlayerAction()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (!CheckGameMaster(ctx, "pre-end player action"))
            return;

        await _manager.CancelPlayerActionAsync(ctx);
    }
    
    [HubMethodName("stopGame")]
    public async Task OnBackToLobby()
    {
        GameContext ctx = (await _manager.GetGameById(Context.User!.GetGameId()))!;
        if (!CheckGameMaster(ctx, "start game"))
            return;
        
        await _manager.StopGameAsync(ctx);
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