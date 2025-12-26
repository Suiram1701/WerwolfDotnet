using System.Security.Cryptography;

namespace WerwolfDotnet.Server.Game;

public class Player
{
    /// <summary>
    /// The id of this player.
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// The displayed name of the player.
    /// </summary>
    public string Name { get; }

    private byte[] _authSecretHash;
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
}