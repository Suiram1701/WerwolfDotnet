using System.Diagnostics;

namespace WerwolfDotnet.Actions;

/// <summary>
/// Represents a current 
/// </summary>
[DebuggerDisplay($"Type = {{{nameof(Type)}}}, Votes = {{{nameof(PlayerVotes)}.Count}} / {{{nameof(Participants)}.Count}}")]
public sealed class PlayerAction(CancellationToken ct)
{
    /// <summary>
    /// The name of this action (localizer key)
    /// </summary>
    public required ActionType Type { get; init; }
    
    /// <summary>
    /// The minimum amount of players to select per player. default: 1
    /// </summary>
    public int Minimum { get; init; } = 1;

    /// <summary>
    /// The maximum amount of players to select per player. default: 1
    /// </summary>
    public int Maximum { get; init; } = 1;

    /// <summary>
    /// Indicates whether to exclude the voter it's self from being voted.
    /// </summary>
    public bool ExcludeSelf { get; init; } = false;
        
    /// <summary>
    /// Specifies players who can be voted.
    /// </summary>
    public required IReadOnlyCollection<Player> VotablePlayers { get; init; }
    
    /// <summary>
    /// Players who participate on the same action.
    /// </summary>
    public required IReadOnlyCollection<Player> Participants { get; init; }

    /// <summary>
    /// Contains the current voting state. Maps voting player to his votes.
    /// </summary>
    public IReadOnlyDictionary<Player, Player[]> PlayerVotes => _playerVotes.AsReadOnly();
    private readonly Dictionary<Player, Player[]> _playerVotes = new();

    public CancellationToken CancellationToken => _cts.Token;
    private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    
    internal CancellationToken OrgCancellationToken => ct;
    
    public event Action<PlayerAction, CancellationToken>? OnCompleted;
    
    public bool RegisterVote(Player self, Player[] selection)
    {
        if (selection.Length < Minimum || selection.Length > Maximum)
            return false;
        if (selection.Any(p => !VotablePlayers.Contains(p)))
            return false;

        _playerVotes[self] = selection;
        
        if (PlayerVotes.Count == Participants.Count && PlayerVotes.All(v => v.Value.Length >= Minimum))
            OnCompleted?.Invoke(this, CancellationToken);
        return true;
    }

    public void CancelAction()
    {
        foreach (Player participant in Participants)
        {
            if (!_playerVotes.ContainsKey(participant))     // Add at least every participant to the dict.
                _playerVotes[participant] = [];
        }
        
        _cts.Cancel();
        OnCompleted?.Invoke(this, CancellationToken);
    }

    public IReadOnlyDictionary<Player, int> GetPlayersByVoteCount(Player[]? doubleValuedVotes = null)
    {
        Dictionary<Player, int> votesForPlayer = new();
        foreach ((Player player, Player[] votesFor) in _playerVotes)
        {
            int value = doubleValuedVotes?.Contains(player) ?? false ? 2 : 1;
            foreach (Player votedOne in votesFor)
            {
                if (votesForPlayer.TryGetValue(votedOne, out int count))
                    votesForPlayer[votedOne] = count + value;
                else
                    votesForPlayer[votedOne] = value;
            }
        }

        return votesForPlayer.AsReadOnly();
    }
    
    /// <summary>
    /// Calculates the player who was voted the most.
    /// </summary>
    /// <param name="doubleValuedVotes">A collection of players whose votes a valued double.</param>
    /// <returns>The Most voted player. When <c>null</c> its a tie.</returns>
    public Player? GetMostVotedPlayer(Player[]? doubleValuedVotes = null)
    {
        var abstention = 0;
        Dictionary<Player, int> votesForPlayer = new();
        foreach ((Player player, Player[] votesFor) in _playerVotes)
        {
            int value = doubleValuedVotes?.Contains(player) ?? false ? 2 : 1;
            if (votesFor.Length == 0)
                abstention += value;
            
            foreach (Player votedOne in votesFor)
            {
                if (votesForPlayer.TryGetValue(votedOne, out int count))
                    votesForPlayer[votedOne] = count + value;
                else
                    votesForPlayer[votedOne] = value;
            }
        }

        int maxVotes = votesForPlayer.Values.Count > 0 ? votesForPlayer.Values.Max() : 0;
        if (abstention >= maxVotes)
            return null;

        Player[] mostVotedOnes = [..votesForPlayer
            .Where(kvp => kvp.Value == maxVotes)
            .Select(kvp => kvp.Key)];
        return mostVotedOnes.Length == 1 ? mostVotedOnes[0] : null;
    }
}