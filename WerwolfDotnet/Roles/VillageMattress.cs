using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.VillageMattress)]
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
            ctx.ProtectPlayer(self, self);
            ctx.Logger.LogTrace("Village mattress {mattress} stays overnight at {player}.", self, LastSleepover);
            
            return Task.FromResult<string[]?>(null);
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}