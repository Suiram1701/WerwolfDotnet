using WerwolfDotnet.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.Healer)]
public sealed class Healer : RoleBase
{
    public Player? LastPlayer { get; private set; }
        
    internal override Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        return ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.HealerSelection,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive && !p.Equals(LastPlayer))],
        }, (action, _) =>
        {
            Player? playerToHeal = action.PlayerVotes[self].SingleOrDefault();
            if (playerToHeal is not null)
            {
                ctx.ProtectPlayer(playerToHeal, self);
                ctx.Logger.Log(Event.Protect, source: self, targets: [playerToHeal]);
            }
            
            LastPlayer = playerToHeal;
            return Task.FromResult<string[]?>(null);
        });
    }
}