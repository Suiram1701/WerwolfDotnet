using System.Security.Cryptography;
using System.Text;

namespace WerwolfDotnet.Server.Game;

/// <summary>
/// A whole context of a game. Contains everything.
/// </summary>
public class GameContext
{
    /// <summary>
    /// The ID of this game session. Should always be formatted using .ToString("D6")
    /// </summary>
    public int Id { get; private init; }

    /// <summary>
    /// Indicates whether this session is password protected.
    /// </summary>
    public bool IsProtected => _passwordHash != null;
    private byte[]? _passwordHash = null;

    /// <summary>
    /// The current state of the game. Indicates everything from whether it's running to which role is next.
    /// </summary>
    public GameState State { get; private set; }
    
    private GameContext()     // Makes constructor private
    {
    }

    /// <summary>
    /// Creates a new game session. (Only an instance, not joinable)
    /// </summary>
    /// <param name="password">An optional password to protect the session with.</param>
    /// <returns>The created session.</returns>
    public static GameContext Create(string? password)
    {
        var gameId = (int)Random.Shared.NextInt64(0, 999999);
        byte[]? passwordHash = null;
        
        if (!string.IsNullOrEmpty(password))
        {
            byte[] pwdBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = new byte[pwdBytes.Length + 4];     // int32 has 4 bytes
            Buffer.BlockCopy(pwdBytes, 0, hashBytes, 0, pwdBytes.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(gameId), 0, hashBytes, pwdBytes.Length, 4);
            
            passwordHash = SHA256.HashData(hashBytes);
        }

        return new GameContext()
        {
            Id = gameId,
            _passwordHash = passwordHash
        };
    }
}