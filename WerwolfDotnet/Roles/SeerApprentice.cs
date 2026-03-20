namespace WerwolfDotnet.Roles;

[Role(Role.SeerApprentice)]
public sealed class SeerApprentice : Seer
{
    /// <summary>
    /// Indicates whether this apprentice is currently active.
    /// </summary>
    /// <remarks>
    /// An apprentice activates when every <see cref="Seer"/> is dead and every
    /// <see cref="SeerApprentice"/> in the player list before him are also dead.
    /// </remarks>
    public bool IsActive { get; private set; }
    
    internal override Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        bool otherActive = !IsActive && ctx.Players.Where(p => p.Role is Seer).Any(p =>
        {
            if (p.Role is SeerApprentice apprentice)
                return p.IsAlive && apprentice.IsActive;
            return p.IsAlive;
        });
        if (otherActive)
            return Task.CompletedTask;

        IsActive = true;
        return base.OnNightAsync(ctx, self, ct);
    }

    internal override Task OnDeathAsync(GameContext ctx, Player self, CauseOfDeath cause, CancellationToken ct)
    {
        IsActive = false;
        return base.OnDeathAsync(ctx, self, cause, ct);
    }
}