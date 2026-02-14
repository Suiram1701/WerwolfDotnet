namespace WerwolfDotnet.Roles;

public sealed class Witch : RoleBase
{
    public override Role Type => Role.Witch;

    public bool CanHeal { get; private set; } = true;

    public bool CanKill { get; private set; } = true;

    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (CanHeal && ctx.Players.Any(p => p.Status == PlayerState.PendingDeath))
        {
            // Healing
            await ctx.RequestPlayerActionAsync(new PhaseAction
            {
                Type = ActionType.WitchHealSelection,
                Minimum = 0,
                Maximum = 1,
                Participants = [self],
                VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.PendingDeath)]
            }, (action, _) =>
            {
                if (action.PlayerVotes[self].SingleOrDefault() is { } playerToHeal)
                {
                    playerToHeal.Revive(self);
                    CanHeal = false;
                }
                return Task.FromResult<string[]?>(null);
            }, ct);
        }

        if (CanKill)
        {
            // Killing
            await ctx.RequestPlayerActionAsync(new PhaseAction
            {
                Type = ActionType.WitchKillSelection,
                Minimum = 0,
                Maximum = 1,
                Participants = [self],
                // ExcludeSelf = true,     // Why not allow the witch to kill herself :)
                VotablePlayers = [..ctx.Players.Where(p => p.IsAlive)]
            }, (action, _) =>
            {
                if (action.PlayerVotes[self].SingleOrDefault() is { } playerToKill)
                {
                    playerToKill.Kill(CauseOfDeath.WitchPoisoning, self);
                    CanKill = false;
                }
                return Task.FromResult<string[]?>(null);
            }, ct);
        }
        
        await base.OnNightAsync(ctx, self, ct);
    }

    internal override Task OnDeathAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (!ctx.GameOptions!.ExplodingWitchHome)
            return Task.CompletedTask;
        
        int i = ctx.Players.Index().Single(t => t.Item.Equals(self)).Index;
        ctx.Players[i <= 0 ? ctx.Players.Count - 1 : i - 1].Kill(CauseOfDeath.WitchExplosion, self);     // Player before the witch
        ctx.Players[i >= ctx.Players.Count - 1 ? 0 : i + 1].Kill(CauseOfDeath.WitchExplosion, self);     // Player after the witch
        return Task.CompletedTask;
    }
}