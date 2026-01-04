using System.Security.Cryptography;
using System.Text;

namespace WerwolfDotnet.Server.Game;

/// <summary>
/// A whole context of a game. Contains everything.
/// </summary>
public sealed class GameContext : IEquatable<GameContext>, IDisposable
{
    /// <summary>
    /// The ID of this game session. Should always be formatted using .ToString("D6")
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Indicates whether this session is password protected.
    /// </summary>
    public bool IsProtected => _passwordHash != null;
    private readonly byte[]? _passwordHash = null;

    /// <summary>
    /// The player who owns the game
    /// </summary>
    /// <remarks>
    /// <c>null</c> while <see cref="State"/> is in <see cref="GameState"/>.NotInitialized
    /// </remarks>
    public Player GameMaster { get; private set; } = null!;

    public Player? Mayor { get; private set; }
    
    /// <summary>
    /// A sorted collection (in seating order) of all players.
    /// </summary>
    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    private readonly List<Player> _players = [];
    
    public int MaxPlayers { get; }

    /// <summary>
    /// The current state of the game. Indicates everything from whether it's running to which role is next.
    /// </summary>
    public GameState State { get; private set; } = GameState.NotInitialized;

    private readonly ILogger _logger;

    /// <summary>
    /// Create a new instance
    /// </summary>
    /// <param name="id">The id of this instance</param>
    /// <param name="password">An optional password which protects this session.</param>
    /// <param name="maxPlayers">The maximum amount of players.</param>
    /// <param name="logger">A logger the game will use to log various things.</param>
    public GameContext(int id, string? password, int maxPlayers, ILogger logger)     // Makes constructor private
    {
        Id = id;
        if (!string.IsNullOrEmpty(password))
        {
            byte[] pwdBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = new byte[pwdBytes.Length + 4];     // int32 has 4 bytes
            Buffer.BlockCopy(pwdBytes, 0, hashBytes, 0, pwdBytes.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(Id), 0, hashBytes, pwdBytes.Length, 4);
            
            _passwordHash = SHA256.HashData(hashBytes);
        }

        MaxPlayers = maxPlayers;
        _logger = logger;
    }
    
    public void InitializeGame(Player gameMaster)
    {
        if (GameMaster is not null || State != GameState.NotInitialized)
            throw new InvalidOperationException("The game were already initialized!");
        
        GameMaster = gameMaster;
        _players.Add(gameMaster);
        State = GameState.Preparation;
    }

    public bool VerifyPassword(string? password)
    {
        ThrowWhenNotInit();

        if (_passwordHash is null)
            return true;
        
        byte[] pwdBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
        var hashBytes = new byte[pwdBytes.Length + 4];
        Buffer.BlockCopy(pwdBytes, 0, hashBytes, 0, pwdBytes.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(Id), 0, hashBytes, pwdBytes.Length, 4);

        return SHA256.HashData(hashBytes).SequenceEqual(_passwordHash);
    }

    public void AddPlayer(Player player)
    {
        ThrowWhenNotInit();
        _players.Add(player);
        _logger.LogInformation("Player {playerName} ({playerId}) joined the game", player.Name, player.Id);
    }

    public bool RemovePlayer(Player player)
    {
        ThrowWhenNotInit();
        
        if (!_players.Remove(player))
            return false;
        _logger.LogInformation("Player {playerName} ({playerId}) left the game", player.Name, player.Id);

        if (player.Equals(GameMaster))
        {
            Player? newGm = _players.MinBy(p => p.Id);     // Elected by the id -> the one who joined after the GM
            if (newGm is not null)
            {
                GameMaster = newGm;
                _logger.LogInformation("Game master left the game. New game master {newGm} ({newGmId}) selected.", newGm.Name, newGm.Id);
            }
            else
            {
                Dispose();     // No one else is part of the game. 
                _logger.LogInformation("Game master left the game. No one else could be selected as new GM -> Disposing game...");
            }
        }
        
        return true;
    }
    
    private void ThrowWhenNotInit()
    {
        if (State == GameState.NotInitialized)
            throw new InvalidOperationException("A game hasn't been initialized yet!");
    }

    public bool Equals(GameContext? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is GameContext other && Equals(other);

    public override int GetHashCode() => Id;
    
    public void Dispose()
    {
        State = GameState.NotInitialized;
    }
}