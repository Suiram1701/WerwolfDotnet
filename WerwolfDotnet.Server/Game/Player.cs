using System.Diagnostics;
using System.Security.Cryptography;
using WerwolfDotnet.Server.Game.Roles;

namespace WerwolfDotnet.Server.Game;

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
    /// The role the player has at the time. Only null when the game hasn't started yet
    /// </summary>
    public IRole? Role { get; internal set; }

    /// <summary>
    /// The current status of the player.
    /// </summary>
    public PlayerState Status { get; internal set; } = PlayerState.Alive;
    
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