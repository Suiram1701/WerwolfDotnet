namespace WerwolfDotnet;

/// <summary>
/// Options for an action requested from a player,
/// </summary>
public class ActionOptions
{
    /// <summary>
    /// The name of this action (localizer key)
    /// </summary>
    public required string ActionName { get; init; }
    
    /// <summary>
    /// The description of this action (localizer key)
    /// </summary>
    public required string ActionDesc { get; init; }
    
    /// <summary>
    /// The minimum amount of players to select
    /// </summary>
    public int Minimum { get; init; } = 1;

    /// <summary>
    /// The maximum amount of players to select.
    /// </summary>
    public int Maximum { get; init; } = 1;

    /// <summary>
    /// Ids of player that can't be selected.
    /// </summary>
    public int[] ExcludedPlayers { get; init; } = [];
}