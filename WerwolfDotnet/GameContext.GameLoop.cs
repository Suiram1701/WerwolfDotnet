namespace WerwolfDotnet;

partial class GameContext
{
    private async Task _runAsync(CancellationToken ct)
    {
        State = GameState.Night;
        OnGameStateChanged?.Invoke(this, State, []);
        
        while (!ct.IsCancellationRequested)
        {
            await _runNightAsync(ct);
            _switchBetweenMainStates(GameState.Day);
            
            await _runDayAsync(ct);
            _switchBetweenMainStates(GameState.Day);
        }
    }
    
    private async Task _runNightAsync(CancellationToken ct)
    {
    }

    private async Task _runDayAsync(CancellationToken ct)
    {
    }
    
    private void _switchBetweenMainStates(GameState newState)
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