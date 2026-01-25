using WerwolfDotnet.Roles;

namespace WerwolfDotnet;

partial class GameContext
{
    private async Task _RunAsync(CancellationToken ct)
    {
        State = GameState.Night;
        OnGameStateChanged?.Invoke(this, State, []);
        
        while (!ct.IsCancellationRequested)
        {
            await _RunNightAsync(ct);
            _EvaluatePreviousState(GameState.Day);
            
            await _RunDayAsync(ct);
            _EvaluatePreviousState(GameState.Night);
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
    }

    private async Task _HandleWerwolfsAsync(CancellationToken ct)
    {
        await RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.WerwolfVoting,
            ExcludeParticipants = true,
            Participants = [.._players.Where(p => p.IsAlive && p.Role!.Type == Role.Werwolf)]
        });

        if (RunningAction!.GetMostVotedPlayer() is { } playerToDie)
        {
            playerToDie.Status = PlayerState.PendingDeath;
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
            if (role.CanHeal)
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
                    playerToHeal.Status = PlayerState.Alive;
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
                    playerToKill.Status = PlayerState.PendingDeath;
                    role.CanKill = false;
                }
                CompletePlayerAction();
            }
        }
    }
    
    private void _EvaluatePreviousState(GameState newState)
    {
        State = newState;

        if (_gameOptions!.ExplodingWitchHome)
        {
            foreach (Player diedWitch in _players.Where(p => p.Role!.Type == Role.Witch && p.Status == PlayerState.PendingDeath))
            {
                int i = _players.IndexOf(diedWitch);
                _players[i <= 0 ? _players.Count - 1 : i - 1].Status = PlayerState.PendingDeath;     // Player before the witch
                _players[i >= _players.Count - 1 ? 0 : i + 1].Status = PlayerState.PendingDeath;     // Player after the witch
            }
        }

        Player[] diedPlayers = _players
            .Where(p => p.Status == PlayerState.PendingDeath)
            .Select(p =>
            {
                p.Status = PlayerState.Death;
                return p;
            })
            .ToArray();
        OnGameStateChanged?.Invoke(this, State, diedPlayers);
    }
}