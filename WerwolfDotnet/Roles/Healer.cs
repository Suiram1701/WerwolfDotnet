using WerwolfDotnet.Actions;
using WerwolfDotnet.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.Healer)]
public sealed class Healer : RoleBase
{
    public Player? LastPlayer { get; private set; }
        
    internal override Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        return ctx.RequestPlayerActionAsync(new PlayerAction(ct)
        {
            Type = ActionType.HealerSelection,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive && !p.Equals(LastPlayer))],
        }, (action, _) =>
        {
            LastPlayer = action.PlayerVotes[self].SingleOrDefault();
            if (LastPlayer is null)
                return ActionResult.Failed();
            
            ctx.ProtectPlayer(LastPlayer, self);
            ctx.Logger.Log(Event.Protect, source: self, targets: [LastPlayer]);
            return ActionResult.Success(LastPlayer);
        });
    }
}