using Microsoft.Extensions.Logging;
using WerwolfDotnet.Roles;

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
        await _HandleWerwolfsAsync(ct);
        await _HandleSeersAsync(ct);
        await _HandleWitchesAsync(ct);
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
    }

    private Task _HandleWerwolfsAsync(CancellationToken ct)
    {
        return RequestPlayerActionAsync(new PhaseAction
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

    private async Task _HandleSeersAsync(CancellationToken ct)
    {
        // Do seers after another
        foreach (Player seer in _players.Where(p => p.IsAlive && p.Role!.Type == Role.Seer))
        {
            await RequestPlayerActionAsync(new PhaseAction
            {
                Type = ActionType.SeerSelection,
                ExcludeParticipants = true,
                Participants = [seer]
            }, (action, _) =>
            {
                if (action.GetMostVotedPlayer() is { } selectedOne)
                {
                    Logger.LogTrace(
                        "Seer {seerName} ({seerId}) saw role of {playerName} ({playerId}): {roleName}",
                        seer.Name, seer.Id, selectedOne.Name, selectedOne.Id, selectedOne.Role!.Type);
                    return Task.FromResult<string[]?>([selectedOne.Name, selectedOne.Role!.Type.ToString()]);
                }
                return Task.FromResult<string[]?>(null);
            }, ct);
        }
    }

    private async Task _HandleWitchesAsync(CancellationToken ct)
    {
        foreach (Player witch in _players.Where(p => p.IsAlive && p.Role!.Type == Role.Witch))
        {
            var role = (Witch)witch.Role!;
            if (role.CanHeal && _players.Any(p => p.Status == PlayerState.PendingDeath))
            {
                // Healing
                await RequestPlayerActionAsync(new PhaseAction
                {
                    Type = ActionType.WitchHealSelection,
                    Minimum = 0,
                    Maximum = 1,
                    Participants = [witch],
                    ExcludedPlayers = _players.Where(p => p.Status != PlayerState.PendingDeath)
                }, (action, _) =>
                {
                    if (action.PlayerVotes[witch].SingleOrDefault() is { } playerToHeal)
                    {
                        playerToHeal.Revive(witch);
                        role.CanHeal = false;
                    }
                    return Task.FromResult<string[]?>(null);
                }, ct);
            }

            if (role.CanKill)
            {
                // Killing
                await RequestPlayerActionAsync(new PhaseAction
                {
                    Type = ActionType.WitchKillSelection,
                    Minimum = 0,
                    Maximum = 1,
                    Participants = [witch],
                    // ExcludeParticipants = true,     // Why not allow the witch to kill herself :)
                    ExcludedPlayers = _players.Where(p => !p.IsAlive)
                }, (action, _) =>
                {
                    if (action.PlayerVotes[witch].SingleOrDefault() is { } playerToKill)
                    {
                        playerToKill.Kill(CauseOfDeath.WitchPoisoning, witch);
                        role.CanKill = false;
                    }
                    return Task.FromResult<string[]?>(null);
                }, ct);
            }
        }
    }

    private async Task _HandleHuntersAsync(CancellationToken ct)
    {
        foreach (Player hunter in _players.Where(p => p.Status == PlayerState.PendingDeath && p.Role!.Type == Role.Hunter))
        {
            await RequestPlayerActionAsync(new PhaseAction
            {
                Type = ActionType.HunterSelection,
                Minimum = _gameOptions!.HunterMustKill ? 1 : 0,
                Maximum = 1,
                ExcludeParticipants = true,
                Participants = [hunter]
            }, (action, _) =>
            {
                if (action.GetMostVotedPlayer() is { } selectedOne)
                    selectedOne.Kill(CauseOfDeath.ShootByHunter, hunter);
                return Task.FromResult<string[]?>(null);
            }, ct);
        }
    }
    
    private async Task _EvaluatePreviousStateAsync(GameState nextState, CancellationToken ct)
    {
        await _HandleHuntersAsync(ct);
        
        if (_gameOptions!.ExplodingWitchHome)
        {
            foreach (Player diedWitch in _players.Where(p => p.Role!.Type == Role.Witch && p.Status == PlayerState.PendingDeath))
            {
                int i = _players.IndexOf(diedWitch);
                _players[i <= 0 ? _players.Count - 1 : i - 1].Kill(CauseOfDeath.WitchExplosion, diedWitch);     // Player before the witch
                _players[i >= _players.Count - 1 ? 0 : i + 1].Kill(CauseOfDeath.WitchExplosion, diedWitch);     // Player after the witch
            }
        }

        State = nextState;
        IReadOnlyDictionary<Player, (CauseOfDeath, Role)> diedPlayers = _players
            .Where(p => p.Status == PlayerState.PendingDeath)
            .Select(p =>
            {
                CauseOfDeath cause = p.KillInternal();
                Role displayedRole = _gameOptions.RevealRoleForCauses.Contains(cause) ? p.Role!.Type : Role.None;
                cause = nextState == GameState.Night ? cause : CauseOfDeath.None;     // The displayed caused (censored when switching to day)
                
                return KeyValuePair.Create(p, (cause, displayedRole));
            })
            .ToDictionary();
        OnGameStateChanged?.Invoke(this, State, diedPlayers);
    }
}