using System.Diagnostics;

namespace WerwolfDotnet.Roles;

[DebuggerDisplay($"{{{nameof(Type)}}}")]
public abstract class RoleBase
{
    public abstract Role Type { get; }
    
    internal virtual Task OnDayAsync(GameContext ctx, Player self, CancellationToken ct) => Task.CompletedTask;
    
    internal virtual Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct) => Task.CompletedTask;
    
    internal virtual Task OnDeathAsync(GameContext ctx, Player self, CancellationToken ct) => Task.CompletedTask;
}