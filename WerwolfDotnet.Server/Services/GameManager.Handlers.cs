using WerwolfDotnet.Actions;
using WerwolfDotnet.Logging;
using WerwolfDotnet.Roles;
using WerwolfDotnet.Server.Hubs;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Services;

public partial class GameManager
{
    /// <summary>
    /// Sends an update of the player list.
    /// </summary>
    /// <remarks>
    /// When <paramref name="recipient"/> is <c>null</c> the update a broadcasted and when not
    /// it's adapted to the player (in terms of role visibility) and send to him.
    /// </remarks>
    /// <param name="ctx">The game this action should be done in.</param>
    /// <param name="recipient">An optional recipient of the update.</param>
    /// <returns>A task to await</returns>
    public Task UpdatePlayersAsync(GameContext ctx, Player? recipient)
    {
        var seer = recipient?.Role as Seer;
        
        IEnumerable<PlayerDto> dtos = ctx.Players.ToDtoCollection(p => ctx.State == GameState.GameWon
            ? p.Role?.Type
            : (seer?.WatchedPlayers.TryGetValue(p, out Role r) ?? false) ? r : null);
        return recipient is null
            ? _hubContext.Clients.Game(ctx.Id).PlayersUpdated(dtos)
            : _hubContext.Clients.Player(ctx.Id, recipient.Id).PlayersUpdated(dtos);
    }
    
    /// <summary>
    /// Sends an update of a players role to him. Additionally, player relations are re-evaluated and send.
    /// </summary>
    /// <param name="ctx">The game this is about.</param>
    /// <param name="player">The player to update.</param>
    /// <returns>A task to await.</returns>
    public Task UpdatePlayerRoleAsync(GameContext ctx, Player player)
    {
        if (player.Role is null)
            return Task.CompletedTask;
        
        Dictionary<int, PlayerRelation[]> relations = new();
        if (player.Role?.VisibleAllies.Length > 0)
        {
            foreach (Player ally in ctx.Players.Where(p => player.Role.VisibleAllies.Contains(p.Role!.Type) && !player.Equals(p)))
                relations[ally.Id] = [PlayerRelation.Ally];
        }

        if (ctx.PlayersInLove.TryGetValue(player, out Player? lovedOne))
        {
            if (relations.TryGetValue(lovedOne.Id, out PlayerRelation[]? currentRelation))
                relations[lovedOne.Id] = [..currentRelation, PlayerRelation.Lover];
            else
                relations[lovedOne.Id] = [PlayerRelation.Lover];
        }

        return _hubContext.Clients.Player(ctx.Id, player.Id).PlayerRoleUpdated(player.Role!.Type, relations);
    }

    public Task UpdateFullGameLogAsync(GameContext ctx, Player? recipient)
    {
        LogMessageDto[] shonMessages = ctx.Logger.Messages     // Either the msg is from a previous game, the requesting user was part of the action or global
            .Where(m =>
            {
                if (reveal(m) || m.Event <= 0)
                    return true;
                return m.Event == Event.Voting && m.Args.Skip(1)     // Event.Voting is expected to have a collection key-value-pairs at its second position
                    .Any(arg => ((KeyValuePair<Player, Player[]>)arg!).Key.Id == recipient?.Id);
            })
            .Select(m => new LogMessageDto(m, reveal(m)))
            .ToArray();

        return recipient is null
            ? _hubContext.Clients.Game(ctx.Id).GameLogUpdated(shonMessages)
            : _hubContext.Clients.Player(ctx.Id, recipient.Id).GameLogUpdated(shonMessages);
        
        bool reveal(LogMessage msg) => ctx.State == GameState.GameWon || msg.Time < ctx.RoundStartedAt;     // Show only when the game is finished or from a previous game
    }
    
    // try-catch is "required" everywhere because of async-void 
    private async void LogHandler(LogMessage message, GameContext context, ILogger logger)
    {
        try
        {
            LogLevel level = message.Event switch
            {
                Event.Voting => LogLevel.Trace,
                <= 0 => LogLevel.Information,
                _ => LogLevel.Debug
            };
            string text = message.Event switch
            {
                Event.Joined => "Player {player} joined the game",
                Event.Left => "Player {player} left the game",
                Event.BecameGameMaster => "{oldGm} left and {player} is the new game master",
                Event.GameStarted => "Game started",
                Event.GameStopped => "Game stopped",
                Event.GameWon => "Game was won by {wonBy}:" + spreadArgs(prefix: "winner"),
                Event.Voting => "{actionType} finished: " + spreadArgs(),
                Event.Killed => "{killer} killed " + spreadArgs(prefix: "victim"),
                Event.Healed => "{doneBy} revived {healedOne}",
                Event.SawRole => "{seer} saw current role of {player}: {role}",
                Event.SeerApprenticeActive => "Seer apprentice {newSeer} is now the active seer",
                Event.Protect => "{doneBy} protects {player} in current round",
                Event.SuccessfullyProtected => "{doneBy} successfully protected {player}",
                Event.FallInLove => "{player} made " + spreadArgs() + " to fall in love",
                Event.SleepOver => "{doneBy} stayed overnight at {player}'s place",
                Event.VictimMissed => "{killer} failed (missed) to kill {victim}",
                Event.TurnedToWerwolf => "{doneBy} turned {victim} into a werwolf",
                _ => "[Unknown event]"
            };
            logger.Log(level, text, message.Args);
            
            // Either the msg is from a previous game, the requesting user was part of the action or global
            bool reveal =
                context.State == GameState.GameWon ||
                message.Time < context.RoundStartedAt; // Show only when the game is finished or from a previous game
            if (reveal || message.Event <= 0)
                await _hubContext.Clients.Game(context.Id).GameLogUpdated([new LogMessageDto(message, reveal)]);
            if (message.Event ==
                Event.Voting) // Event.Voting is expected to have a collection key-value-pairs at its second position
                await _hubContext.Clients.Players(context.Id, message.Args.Skip(1).Select(arg => ((KeyValuePair<Player, Player[]>)arg!).Key.Id))
                    .GameLogUpdated([new LogMessageDto(message, reveal)]);
        }
        catch (Exception ex)
        { _logger.LogError(ex, ex.Message); }
        
        string spreadArgs(int alreadyTook = 1, string prefix = "arg") => string.Join(", ",
            Enumerable.Range(alreadyTook, message.Args.Length - alreadyTook).Select(i => $"{{arg{i - alreadyTook}}}"));
    }
    
    private async void OnGameMetadataChangedAsync(GameContext ctx, int gameMasterId, int? mayorId)
    {
        try
        { await _hubContext.Clients.Game(ctx.Id).GameMetaUpdated(gameMasterId, mayorId); }
        catch (Exception ex)
        { _logger.LogError(ex, ex.Message); }
    }
    
    private async void OnGameStateChangedAsync(GameContext ctx, GameState newState, IReadOnlyDictionary<Player, (CauseOfDeath, Role)> diedPlayers, bool? bearGrowls)
    {
        try
        {
            IReadOnlyDictionary<int, DeathDetails> deathOnes = diedPlayers
                .Select(kvp => KeyValuePair.Create(kvp.Key.Id, new DeathDetails(kvp.Value.Item1, kvp.Value.Item2)))
                .ToDictionary();
            await _hubContext.Clients.Game(ctx.Id).GameStateUpdated(newState, deathOnes, bearGrowls);

            if (newState == GameState.GameWon)     // The end state reveals a ton of infos
                await UpdateFullGameLogAsync(ctx, null);
        }
        catch (Exception ex)
        { _logger.LogError(ex, ex.Message); }
    }

    private async void OnPhaseActionAsync(GameContext ctx, PlayerAction action)
    {
        try
        {
            await Task.WhenAll(action.Participants.Select(p => _hubContext.Clients.Player(ctx.Id, p.Id)
                .PlayerActionRequested(new SelectionOptionsDto(action, p))));
        }
        catch (Exception ex)
        { _logger.LogError(ex, ex.Message); }
    }

    private async void OnPlayerActionCompletedAsync(GameContext ctx, PlayerAction action, ActionResult result)
    {
        try
        {
            string[] formatters = result.Results.Select(obj => obj switch
            {
                Player p => p.Name,
                Enum => ((int)obj).ToString(),
                _ => throw new ArgumentException($"Argument type {obj.GetType()} isn't supported!", nameof(obj))     // Make sure not return not reviewed objects
            }).ToArray();
            await _hubContext.Clients.Players(ctx.Id, action.Participants.Select(p => p.Id)).PlayerActionCompleted(formatters);

            if (!action.PlayerVotes.Values.Any(l => l.Length > 0))
                return;
            switch (action.Type)     // Some actions have side effects on other players
            {
                case ActionType.UrwolfSelection:
                    await Task.WhenAll(ctx.Players.Where(p => p.Role!.Type < 0).Select(p => UpdatePlayerRoleAsync(ctx, p)));
                    break;
                case ActionType.SeerSelection:
                case ActionType.SeerApprenticeSelection:
                    await UpdatePlayersAsync(ctx, action.Participants.Single());     // Refreshes shown roles
                    break;
                case ActionType.AmorSelection:
                    await Task.WhenAll(action.PlayerVotes.Values
                        .SelectMany(l => l)
                        .Select(p => UpdatePlayerRoleAsync(ctx, p)));
                    break;
            }
        }
        catch (Exception ex)
        { _logger.LogError(ex, ex.Message); }
    }

    private async void OnGameWonAsync(GameContext ctx, Fraction fraction)
    {
        try
        {
            await _hubContext.Clients.Game(ctx.Id).GameWon(fraction);
            await UpdatePlayersAsync(ctx, null);     // Trigger update to load the roles
        }
        catch (Exception ex)
        { _logger.LogError(ex, ex.Message); }
    }
}