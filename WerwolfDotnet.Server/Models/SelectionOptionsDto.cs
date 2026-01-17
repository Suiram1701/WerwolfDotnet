namespace WerwolfDotnet.Server.Models;

public class SelectionOptionsDto(PhaseAction action, Player self)
{
    /// <summary>
    /// The name of this action (localizer key)
    /// </summary>
    public string ActionName { get; } = action.ActionName;

    /// <summary>
    /// The description of this action (localizer key)
    /// </summary>
    public string ActionDesc { get; } = action.ActionDesc;

    /// <summary>
    /// The minimum amount of players to select per player.
    /// </summary>
    public int Minimum { get; } = action.Minimum;
    
    /// <summary>
    /// The maximum amount of players to select per player.
    /// </summary>
    public int Maximum { get; } = action.Maximum;

    public int[] ExcludedPlayers { get; } = Enumerable.Empty<int>()
        .Concat(action.ExcludeSelf ? [self.Id] : [])
        .Concat(action.ExcludeParticipants ? action.Participants.Select(p => p.Id) : [])
        .Distinct()
        .ToArray();
}