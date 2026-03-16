using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

public sealed class Healer : RoleBase
{
    public override Role Type => Role.Healer;

    /// <summary>
    /// The recently protected players.
    /// </summary>
    public Player[] LastPlayers => _lastPlayers.ToArray();
    private readonly Queue<Player> _lastPlayers = new(2);
        
    internal override Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        return ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.HealerSelection,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Except(_lastPlayers).Where(p => p.Status == PlayerState.Alive)],
        }, (action, _) =>
        {
            if (_lastPlayers.Count == 2)
                _lastPlayers.Dequeue();
            if (action.PlayerVotes[self].SingleOrDefault() is { } playerToHeal)
            {
                ctx.ProtectPlayer(playerToHeal, self);
                ctx.Logger.LogTrace("Healer {self} choose {playerToHeal} to protect.", self, playerToHeal);
                
                _lastPlayers.Enqueue(playerToHeal);
            }
            return Task.FromResult<string[]?>(null);
        });
    }
}