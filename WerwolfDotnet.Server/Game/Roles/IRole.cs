namespace WerwolfDotnet.Server.Game.Roles;

public interface IRole
{
    /// <summary>
    /// A simple but unique name used for the role.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Called when this role has to take action in the night.
    /// </summary>
    /// <param name="ctx">Game context</param>
    /// <param name="self">The player this role belongs to.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task to await</returns>
    Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct);
}