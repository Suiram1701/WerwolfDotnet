namespace WerwolfDotnet.Server.Models;

public class SelectionOptionsDto(PhaseAction action, Player self)
{
    /// <summary>
    /// The name of this action (localizer key)
    /// </summary>
    public ActionType Type { get; } = action.Type;

    /// <summary>
    /// The minimum amount of players to select per player.
    /// </summary>
    public int Minimum { get; } = action.Minimum;
    
    /// <summary>
    /// The maximum amount of players to select per player.
    /// </summary>
    public int Maximum { get; } = action.Maximum;

    public int[] ExcludedPlayers { get; } = action.ExcludedPlayers
        .Concat(action.ExcludeSelf ? [self] : [])
        .Concat(action.ExcludeParticipants ? action.Participants : [])
        .Select(p => p.Id)
        .Distinct()
        .ToArray();
}