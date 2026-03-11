using System.Diagnostics;

namespace WerwolfDotnet.Roles;

[DebuggerDisplay($"{{{nameof(Type)}}}")]
public abstract class RoleBase
{
    public abstract Role Type { get; }

    public virtual bool AlliesVisible => false;
    
    internal virtual Task OnDayAsync(GameContext ctx, Player self, CancellationToken ct) => Task.CompletedTask;
    
    internal virtual Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct) => Task.CompletedTask;

    internal virtual Task OnDeathAsync(GameContext ctx, Player self, CauseOfDeath cause, CancellationToken ct)
    {
        ctx.PlayersInLove.TryGetValue(self, out Player? lovedOne);
        lovedOne?.Kill(CauseOfDeath.DeathByHearthBreak, self);

        if (cause != CauseOfDeath.WerwolfKill)
            return Task.CompletedTask;
        foreach (Player mattress in ctx.Players.Where(p => p.Role is VillageMattress m && self.Equals(m.LastSleepover)))
            mattress.Kill(cause, null);

        return Task.CompletedTask;
    }
}