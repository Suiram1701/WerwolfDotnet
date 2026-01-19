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
            return;
            _SwitchBetweenMainStates(GameState.Day);
            
            await _RunDayAsync(ct);
            _SwitchBetweenMainStates(GameState.Day);
        }
    }
    
    private async Task _RunNightAsync(CancellationToken ct)
    {
        await _HandleWerwolfsAsync(ct);
        await _HandleSeersAsync(ct);
    }

    private async Task _RunDayAsync(CancellationToken ct)
    {
    }

    private async Task _HandleWerwolfsAsync(CancellationToken ct)
    {
        await RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.WerwolfVoting,
            ExcludeParticipants = true,
            Participants = [.._players.Where(p => p.IsAlive && p.Role!.Type == Role.Werwolf)]
        });

        Dictionary<Player, int> grouped = RunningAction!.PlayerVotes.Values
            .SelectMany(p => p)
            .GroupBy(p => p)
            .ToDictionary(p => p.Key, group => group.Count());
        int maxVotes = grouped.Values.Max();
        if (grouped.Values.Count(v => v == maxVotes) == 1)
        {
            Player playerToDie = grouped.MaxBy(kvp => kvp.Value).Key;
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

            Player playerToReveal = RunningAction!.PlayerVotes[seer].Single();     // Guaranteed by PhaseAction
            CompletePlayerAction([playerToReveal.Name, playerToReveal.Role!.Type.ToString()]);
        }
    }
    
    private void _SwitchBetweenMainStates(GameState newState)
    {
        State = newState;

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