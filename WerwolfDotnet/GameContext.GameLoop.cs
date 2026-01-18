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
            _SwitchBetweenMainStates(GameState.Day);
            return;
            
            await _RunDayAsync(ct);
            _SwitchBetweenMainStates(GameState.Day);
        }
    }
    
    private async Task _RunNightAsync(CancellationToken ct)
    {
        await _HandleWerwolfsAsync(ct);
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
            Participants = [.._players.Where(p => p.IsKillable && p.Role!.Type == Role.Werwolf)]
        });

        Dictionary<Player, int> playersVoted = new();
        foreach ((_, Player[] votedPlayers) in RunningAction!.PlayerVotes)
        {
            foreach (Player votedOne in votedPlayers)
            {
                int votes = playersVoted.GetValueOrDefault(votedOne, 0);
                votes++;
                playersVoted[votedOne] = votes;
            }
        }

        Player playerToDie = playersVoted.MaxBy(kvp => kvp.Value).Key;
        playerToDie.Status = PlayerState.PendingDeath;
        CompletePlayerAction([playerToDie.Name]);
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