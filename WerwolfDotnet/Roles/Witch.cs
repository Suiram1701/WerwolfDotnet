using WerwolfDotnet.Actions;

namespace WerwolfDotnet.Roles;

[Role(Role.Witch)]
public sealed class Witch : RoleBase
{
    public bool CanHeal { get; private set; } = true;

    public bool CanKill { get; private set; } = true;

    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (CanHeal)
        {
            // Healing
            await ctx.RequestPlayerActionAsync(new PlayerAction(ct)
            {
                Type = ActionType.WitchHealSelection,
                Minimum = 0,
                Maximum = 1,
                Participants = [self],
                VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.PendingDeath && !ctx.WerwolfProtectedPlayers.ContainsKey(p))]
            }, (action, _) =>
            {
                if (action.PlayerVotes[self].SingleOrDefault() is not { } playerToHeal)
                    return ActionResult.Success();
                
                playerToHeal.Revive(self);
                CanHeal = false;
                return ActionResult.Success(playerToHeal);
            });
        }

        if (CanKill)
        {
            // Killing
            await ctx.RequestPlayerActionAsync(new PlayerAction(ct)
            {
                Type = ActionType.WitchKillSelection,
                Minimum = 0,
                Maximum = 1,
                Participants = [self],
                // ExcludeSelf = true,     // Why not allow the witch to kill herself :)
                VotablePlayers = [..ctx.Players.Where(p => p.IsAlive)]
            }, (action, _) =>
            {
                if (action.PlayerVotes[self].SingleOrDefault() is not { } playerToKill)
                    return ActionResult.Success();
                
                playerToKill.Kill(CauseOfDeath.WitchPoisoning, self);
                CanKill = false;
                return ActionResult.Success(playerToKill);
            });
        }
        
        await base.OnNightAsync(ctx, self, ct);
    }

    internal override Task OnDeathAsync(GameContext ctx, Player self, CauseOfDeath cause, CancellationToken ct)
    {
        if (!ctx.GameOptions!.ExplodingWitchHome)
            return base.OnDeathAsync(ctx, self, cause, ct);
        
        int i = ctx.Players.Index().Single(t => t.Item.Equals(self)).Index;
        ctx.Players[i <= 0 ? ctx.Players.Count - 1 : i - 1].Kill(CauseOfDeath.WitchExplosion, self);     // Player before the witch
        ctx.Players[i >= ctx.Players.Count - 1 ? 0 : i + 1].Kill(CauseOfDeath.WitchExplosion, self);     // Player after the witch
        return base.OnDeathAsync(ctx, self, cause, ct);
    }
}