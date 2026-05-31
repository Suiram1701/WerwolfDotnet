using WerwolfDotnet.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.VillageMattress, FixedAmount = 1)]     // Not limited by a rule, but more than one wouldn't make sense in terms of sleepover logic
public sealed class VillageMattress : RoleBase
{
    public Player? LastSleepover { get; private set; }

    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.VillageMattressSelection,
            Participants = [self],
            ExcludeSelf = true,
            VotablePlayers = [..ctx.Players.Where(p => p.IsAlive && !p.Equals(LastSleepover))]
        }, (action, _) =>
        {
            LastSleepover = action.PlayerVotes[self].FirstOrDefault();
            if (LastSleepover is not null)
            {
                ctx.ProtectPlayer(self, self);
                ctx.Logger.Log(Event.SleepOver, source: self, target: LastSleepover);
            }
            
            return Task.FromResult<string[]?>(null);
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}