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
            });
            
            if (RunningAction!.GetMostVotedPlayer() is { } newMayor)
            {
                Mayor = newMayor;
                OnGameMetadataChanged?.Invoke(this, GameMaster.Id, newMayor.Id);
                CompletePlayerAction([newMayor.Name]);
            }
            else
            {
                CompletePlayerAction([]);
            }
        }
        
        await RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.WerwolfKilling,
            Minimum = 0,
            Maximum = 1,
            Participants = [.._players.Where(p => p.IsAlive)]
        });

        if (RunningAction!.GetMostVotedPlayer() is { } playerToExecute)
            playerToExecute.Kill(CauseOfDeath.WerwolfKilling, null);
        CompletePlayerAction();
    }

    private async Task _HandleWerwolfsAsync(CancellationToken ct)
    {
        await RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.WerwolfSelection,
            ExcludeParticipants = true,
            Participants = [.._players.Where(p => p.IsAlive && p.Role!.Type == Role.Werwolf)]
        });

        if (RunningAction!.GetMostVotedPlayer() is { } playerToDie)
        {
            playerToDie.Kill(CauseOfDeath.WerwolfKill, null);
            CompletePlayerAction([playerToDie.Name]);
        }
        else
        {
            CompletePlayerAction([]);     // Empty parameters will indicate that no one died.
        }
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
            });

            if (RunningAction!.GetMostVotedPlayer() is { } selectedOne)
                CompletePlayerAction([selectedOne.Name, selectedOne.Role!.Type.ToString()]);
            else
                CompletePlayerAction();
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
                });
            
                if (RunningAction!.PlayerVotes[witch].SingleOrDefault() is { } playerToHeal)
                {
                    playerToHeal.Revive(witch);
                    role.CanHeal = false;
                }
                CompletePlayerAction();
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
                });
            
                if (RunningAction!.PlayerVotes[witch].SingleOrDefault() is { } playerToKill)
                {
                    playerToKill.Kill(CauseOfDeath.WitchPoisoning, witch);
                    role.CanKill = false;
                }
                CompletePlayerAction();
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
            });

            if (RunningAction!.GetMostVotedPlayer() is { } selectedOne)
                selectedOne.Kill(CauseOfDeath.ShootByHunter, hunter);
            CompletePlayerAction();
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