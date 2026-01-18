namespace WerwolfDotnet;

/// <summary>
/// Represents a current 
/// </summary>
public sealed class PhaseAction
{
    /// <summary>
    /// The name of this action (localizer key)
    /// </summary>
    public required ActionType Type { get; init; }
    
    /// <summary>
    /// The minimum amount of players to select per player.
    /// </summary>
    public int Minimum { get; init; } = 1;

    /// <summary>
    /// The maximum amount of players to select per player.
    /// </summary>
    public int Maximum { get; init; } = 1;

    /// <summary>
    /// Indicates whether the one 
    /// </summary>
    public bool ExcludeSelf { get; init; } = false;
    
    /// <summary>
    /// Indicates whether other participants can't be selected.
    /// </summary>
    public bool ExcludeParticipants { get; init; } = false;
    
    /// <summary>
    /// Players who participate on the same action.
    /// </summary>
    public required IReadOnlyCollection<Player> Participants { get; init; }

    /// <summary>
    /// Contains the current voting state. Maps voting player to his votes.
    /// </summary>
    public IReadOnlyDictionary<Player, Player[]> PlayerVotes => _playerVotes.AsReadOnly();
    private readonly Dictionary<Player, Player[]> _playerVotes = new();

    public event Action<PhaseAction>? OnCompleted;
    
    public bool RegisterVote(Player self, Player[] selection)
    {
        if (selection.Length < Minimum || selection.Length > Maximum)
            return false;
        if (ExcludeSelf && selection.Contains(self))
            return false;
        if (ExcludeParticipants && selection.Any(p => Participants.Contains(p)))
            return false;
        
        _playerVotes[self] = selection;

        if (PlayerVotes.Count == Participants.Count && PlayerVotes.All(v => v.Value.Length > 0))
            OnCompleted?.Invoke(this);
        return true;
    }
}