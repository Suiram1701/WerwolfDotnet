using WerwolfDotnet.Actions;
using WerwolfDotnet.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.Amor, FixedAmount = 1)]
public sealed class Amor : RoleBase
{
    public bool Done { get; private set; }

    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (Done)
        {
            await base.OnNightAsync(ctx, self, ct);
            return;
        }

        await ctx.RequestPlayerActionAsync(new PlayerAction(ct)
        {
            Type = ActionType.AmorSelection,
            Minimum = 2,
            Maximum = 2,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive)]     // In the first round everyone should be alive but just to be sure
        }, (action, _) =>
        {
            Player[] votes = action.PlayerVotes[self];
            if (votes.Length != 2)
                return ActionResult.Failed();
            
            ctx.PlayersFallInLove(votes[0], votes[1]);
            ctx.Logger.Log(Event.FallInLove, source: self, targets: [votes[0], votes[1]]);
            
            Done = true;
            return ActionResult.Success();
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}