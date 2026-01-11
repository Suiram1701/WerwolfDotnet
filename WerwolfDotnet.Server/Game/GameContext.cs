using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WerwolfDotnet.Server.Game.Roles;

namespace WerwolfDotnet.Server.Game;

/// <summary>
/// A whole context of a game. Contains everything.
/// </summary>
[DebuggerDisplay($"Game: Code = {nameof(Id)}, Game-master = {nameof(GameMaster)}.{nameof(GameMaster.Id)}, Players = {nameof(Players)}.{nameof(Players.Count)}")]
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
    private List<Player> _players = [];
    
    public int MaxPlayers { get; }

    /// <summary>
    /// The current state of the game. Indicates everything from whether it's running to which role is next.
    /// </summary>
    public GameState State { get; private set; } = GameState.NotInitialized;

    /// <summary>
    /// Invoked when the state of the game changed. Player[] are the players who died during the previous state.
    /// </summary>
    public event Action<GameContext, GameState, Player[]>? OnGameStateChanged;
    
    /// <summary>
    /// Invoked when a player is requested to take action. When player is null everyone has to take action.
    /// </summary> 
    public event Action<GameContext, Player?, ActionOptions>? OnPlayerActionRequested;
    
    private readonly ILogger _logger;
    private CancellationTokenSource? _gameLoopCts;
    private Task? _gameLoop;
    
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
        OnGameStateChanged?.Invoke(this, State, []);
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

    public bool ToggleJoinLock()
    {
        ThrowWhenNotInit();

        if (State > GameState.Locked)     // Game is already running
            return false;
        State = State != GameState.Locked
            ? GameState.Locked
            : GameState.Preparation;
        OnGameStateChanged?.Invoke(this, State, []);
        
        _logger.LogTrace("Toggled game state to {state}.", State);
        return true;
    }
    
    public void ShufflePlayers()
    {
        ThrowWhenNotInit();
        _players = [.. _players.Shuffle()];
    }

    public void StartGame(RoleOptions options)
    {
        ThrowWhenNotInit();
         if (State > GameState.Locked)
             throw new InvalidOperationException("Game game has already been started!");

         IRole[] roles = [
             ..Enumerable.Repeat<IRole?>(null, options.AmountWerwolfs).Select(_ => new Werwolf()),
             ..Enumerable.Repeat<IRole?>(null, options.AmountSeers).Select(_ => new Seer())
         ];
         roles = [..roles.Shuffle()];
         
         Player[] shuffledPlayers = [.._players.Shuffle()];
         
         int assignmentCount = Math.Min(roles.Length, shuffledPlayers.Length);
         for (var i = 0; i < assignmentCount; i++)
             shuffledPlayers[i].Role = roles[i];
         for (int i = assignmentCount; i < shuffledPlayers.Length; i++)
             shuffledPlayers[i].Role = new Villager();
         
         _gameLoopCts = new CancellationTokenSource();
         _gameLoop = _runAsync(_gameLoopCts.Token);
    }

    public void RegisterPlayerAction(Player self, Player[] selection)
    {
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
        _gameLoopCts?.Cancel();
        _gameLoop?.Dispose();
        
        State = GameState.NotInitialized;
    }
    
    private void ThrowWhenNotInit()
    {
        if (State == GameState.NotInitialized)
            throw new InvalidOperationException("A game hasn't been initialized yet!");
    }
    
    private async Task _runAsync(CancellationToken ct)
    {
        State = GameState.Night;
        OnGameStateChanged?.Invoke(this, State, []);

        OnPlayerActionRequested?.Invoke(this, null, new ActionOptions
        {
            ActionName = "Test",
            ActionDesc = "Test"
        });
        return;
        
        while (!ct.IsCancellationRequested)
        {
            await _runNightAsync(ct);
            _switchBetweenMainStates(GameState.Day);
            
            await _runDayAsync(ct);
            _switchBetweenMainStates(GameState.Day);
        }
    }

    private async Task _runDayAsync(CancellationToken ct)
    {
    }

    private async Task _runNightAsync(CancellationToken ct)
    {
        return;
        // Seer
        await InvokeRoleAsync<Seer>(ct);
        
        // Werwolf
        await InvokeRoleAsync<Werwolf>(ct);
    }

    private void _switchBetweenMainStates(GameState newState)
    {
        State = newState;

        Player[] diedPlayers = _players
            .Where(p => p.Status == PlayerState.PendingDeath)
            .Select(p =>
            {
                p.Status = PlayerState.Death;
                return p;
            })
            .ToArray();
        OnGameStateChanged?.Invoke(this, State, diedPlayers);
    }
    
    private async Task InvokeRoleAsync<T>(CancellationToken ct)
        where T : IRole
    {
        foreach (Player player in _players.Where(p => p.Role?.GetType() == typeof(T)))
            await player.Role!.OnNightAsync(this, player, ct);
    }
}