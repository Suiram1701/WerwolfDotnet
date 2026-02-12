namespace WerwolfDotnet;

partial class GameContext
{
    private async Task _RunAsync(CancellationToken ct)
    {
        State = GameState.Night;
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0));
        
        while (!ct.IsCancellationRequested)
        {
            await _RunNightAsync(ct);
            await _EvaluatePreviousStateAsync(nextState: GameState.Day, ct);
            
            await _RunDayAsync(ct);
            await _EvaluatePreviousStateAsync(nextState: GameState.Night, ct);
        }
    }
    
    private async Task _RunNightAsync(CancellationToken ct)
    {
        foreach (Player player in _players
                     .Where(p => p.IsAlive && GameOptions!.NightExecutionOrder.Contains(p.Role!.Type))
                     .OrderBy(p => GameOptions!.NightExecutionOrder.IndexOf(p.Role!.Type)))
        {
            if (player.Role!.Type == Role.Werwolf)
            {
                // Werwölfe
                await RequestPlayerActionAsync(new PhaseAction
                {
                    Type = ActionType.WerwolfSelection,
                    ExcludeParticipants = true,
                    Participants = [.._players.Where(p => p.IsAlive && p.Role!.Type == Role.Werwolf)]
                }, (action, _) =>
                {
                    if (action.GetMostVotedPlayer() is not { } playerToDie)
                        return Task.FromResult<string[]?>([]);     // Empty parameters will indicate that no one died.
            
                    playerToDie.Kill(CauseOfDeath.WerwolfKill, null);
                    return Task.FromResult<string[]?>([playerToDie.Name]);
                }, ct);
            }
            else
            {
                await player.Role!.OnNightAsync(this, player, ct);
            }
        }
    }

    private async Task _RunDayAsync(CancellationToken ct)
    {
        if (Mayor is null)
        {
            await RequestPlayerActionAsync(new PhaseAction
            {
                Type = ActionType.MayorVoting,
                Minimum = 0,
                Maximum = 1,
                Participants = [.._players.Where(p => p.IsAlive)]
            }, (action, _) =>
            {
                if (action.GetMostVotedPlayer() is { } newMayor)
                {
                    Mayor = newMayor;
                    OnGameMetadataChanged?.Invoke(this, GameMaster.Id, newMayor.Id);
                    return Task.FromResult<string[]?>([newMayor.Name]);
                }
                return Task.FromResult<string[]?>([]);
            }, ct);
        }
        
        await RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.WerwolfKilling,
            Minimum = 0,
            Maximum = 1,
            Participants = [.._players.Where(p => p.IsAlive)]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer(Mayor is not null ? [Mayor] : null) is { } playerToExecute)
                playerToExecute.Kill(CauseOfDeath.WerwolfKilling, null);
            return Task.FromResult<string[]?>(null);
        }, ct);
        
        foreach (Player player in _players
                     .Where(p => p.IsAlive && GameOptions!.NightExecutionOrder.Contains(p.Role!.Type))
                     .OrderBy(p => GameOptions!.NightExecutionOrder.IndexOf(p.Role!.Type)))
        {
            await player.Role!.OnDayAsync(this, player, ct);
        }
    }
    
    private async Task _EvaluatePreviousStateAsync(GameState nextState, CancellationToken ct)
    {
        Dictionary<Player, (CauseOfDeath, Role)> diedPlayers = new();
        bool newDeathPlayer;
        do
        {
            newDeathPlayer = false;
            foreach (Player player in _players.Where(p => p.Status == PlayerState.PendingDeath))
            {
                newDeathPlayer = true;
                
                await player.Role!.OnDeathAsync(this, player, ct);
                if (GameOptions!.MayorDecidesNextMayor && player.Equals(Mayor))     // Mayor dies
                {
                    await RequestPlayerActionAsync(new PhaseAction
                    {
                        Type = ActionType.NextMayorDecision,
                        Minimum = 0,
                        Maximum = 1,
                        Participants = [player],
                        ExcludeParticipants = false,
                        ExcludedPlayers = _players.Where(p => p.Status != PlayerState.Alive)
                    }, (action, _) =>
                    {
                        Mayor = action.PlayerVotes[player].FirstOrDefault();
                        return Task.FromResult<string[]?>(null);
                    }, ct);
                }
                else if (player.Equals(Mayor))
                {
                    Mayor = null;
                }
                
                CauseOfDeath cause = player.KillInternal();
                Role displayedRole = GameOptions!.RevealRoleForCauses.Contains(cause) ? player.Role!.Type : Role.None;
                cause = nextState == GameState.Night ? cause : CauseOfDeath.None;     // The displayed caused (censored when switching to day)

                diedPlayers[player] = (cause, displayedRole);
            }
        } while (newDeathPlayer);     // Loop multiple times over in case other players died during Death-Handler

        State = nextState;
        OnGameStateChanged?.Invoke(this, State, diedPlayers);
    }
}