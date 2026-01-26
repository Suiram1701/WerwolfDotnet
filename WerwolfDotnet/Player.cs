using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet;

[DebuggerDisplay($"Player: Id = {nameof(Id)}, Name = {nameof(Name)}, Status = {nameof(Status)}, Role = {nameof(Role)}")]
public class Player : IEquatable<Player>
{
    /// <summary>
    /// The id of this player.
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// The displayed name of the player.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The role the player has at the time. Only <c>null</c> when the game hasn't started yet
    /// </summary>
    public IRole? Role { get; internal set; }

    /// <summary>
    /// The current status of the player.
    /// </summary>
    public PlayerState Status { get; private set; } = PlayerState.Alive;

    /// <summary>
    /// Indicates whether this player can be selected to kill or can do an action.
    /// </summary>
    public bool IsAlive => Status != PlayerState.Death;

    private CauseOfDeath? _causeOfDeath;
    private readonly byte[] _authSecretHash;
    private readonly GameContext _game;
    
    public Player(int id, string name, GameContext game, out string authSecretStr)
    {
        Id = id;
        Name = name;
        _game = game;

        var authSecret = new byte[32];     // 32 Bytes = 256 Bits
        Random.Shared.NextBytes(authSecret);
        authSecretStr = Convert.ToBase64String(authSecret);
        _authSecretHash = SHA256.HashData(authSecret);
    }

    public bool VerifyAuthToken(string token)
    {
        byte[] tokenBytes = Convert.FromBase64String(token);
        return SHA256.HashData(tokenBytes).SequenceEqual(_authSecretHash);
    }

    internal void Kill(CauseOfDeath causeOfDeath, Player? killer)
    {
        if (Status != PlayerState.Alive)
            return;

        Status = PlayerState.PendingDeath;
        _causeOfDeath = causeOfDeath;
        if (killer is null)
            _game.Logger.LogTrace("Player {name} ({id}) was killed in {causeOfDeath}.", Name, Id, causeOfDeath.ToString());
        else
            _game.Logger.LogTrace("Player {name} ({id}) was killed by {killerName} ({killerId}) in {causeOfDeath}.", Name, Id, killer.Name, killer.Id, causeOfDeath.ToString());
    }

    internal void Revive(Player doneBy)
    {
        if (Status != PlayerState.PendingDeath)
            return;
        Status = PlayerState.Alive;
        _game.Logger.LogTrace("Player {name} ({id}) was saved by {name2} ({id2}).", Name, Id, doneBy.Name, doneBy.Id);
    }

    internal CauseOfDeath KillInternal()
    {
        CauseOfDeath cause = _causeOfDeath ?? CauseOfDeath.None;
        Status = PlayerState.Death;
        return cause;
    }
    
    public bool Equals(Player? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return _game.Equals(other._game) && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return obj.GetType() == GetType() && Equals((Player)obj);
    }

    public override int GetHashCode() => HashCode.Combine(_game.Id, Id);
}