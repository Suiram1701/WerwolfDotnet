using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet;

/// <summary>
/// A whole context of a game. Contains everything.
/// </summary>
[DebuggerDisplay($"Game: {{{nameof(Id)}}}, Game-master = {{{nameof(GameMaster)}.{nameof(GameMaster.Id)}}}, Players = {{{nameof(Players)}.Count}}")]
public sealed partial class GameContext : IEquatable<GameContext>, IDisposable
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
    
    /// <summary>
    /// A sorted collection (in seating order) of all players.
    /// </summary>
    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    private List<Player> _players = [];
    
    /// <summary>
    /// The maximum amount of player allowed in this session.
    /// </summary>
    public int MaxPlayers { get; }

    /// <summary>
    /// The current state of the game. Indicates everything from whether it's running to which role is next.
    /// </summary>
    public GameState State { get; private set; } = GameState.NotInitialized;
    
    public PhaseAction? RunningAction { get; private set; }

    /// <summary>
    /// First int is the game master id and seconds integer is the id of the village mayor.
    /// </summary>
    public event Action<GameContext, int, int?>? OnGameMetadataChanged;
    
    /// <summary>
    /// Invoked when the state of the game changed. (Player, CauseOfDeath) are the players who died during the previous state mapped to the cause of death.
    /// </summary>
    public event Action<GameContext, GameState, IReadOnlyDictionary<Player, (CauseOfDeath, Role)>>? OnGameStateChanged;
    
    /// <summary>
    /// Invoked when one or more players are requested to take action. 
    /// </summary> 
    public event Action<GameContext, PhaseAction>? OnPhaseAction;

    /// <summary>
    /// Invoked when a startet phase action completed. The last param are parameters passed for displaying a result.
    /// When <c>null</c> nothing should be shown.
    /// </summary>
    public event Action<GameContext, PhaseAction, string[]?>? OnPhaseActionCompleted;
    
    internal ILogger Logger { get; }
    
    internal GameOptions? GameOptions { get; private set; }
    
    private CancellationTokenSource? _gameLoopCts;
    private Task? _gameLoop;

    private bool _disposed = false;
    
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
        Logger = logger;
    }
    
    public void InitializeGame(Player gameMaster)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GameContext));
        if (GameMaster is not null || State != GameState.NotInitialized)
            throw new InvalidOperationException("The game were already initialized!");
        
        GameMaster = gameMaster;
        _players.Add(gameMaster);
        State = GameState.Preparation;
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0));
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
        Logger.LogInformation("Player {playerName} ({playerId}) joined the game", player.Name, player.Id);
    }

    public bool RemovePlayer(Player player)
    {
        ThrowWhenNotInit();
        
        if (!_players.Remove(player))
            return false;
        Logger.LogInformation("Player {playerName} ({playerId}) left the game", player.Name, player.Id);

        if (player.Equals(GameMaster))
        {
            Player? newGm = _players.MinBy(p => p.Id);     // Elected by the id -> the one who joined after the GM
            if (newGm is not null)
            {
                GameMaster = newGm;
                OnGameMetadataChanged?.Invoke(this, newGm.Id, Mayor?.Id);
                Logger.LogInformation("Game master left the game. New game master {newGm} ({newGmId}) selected.", newGm.Name, newGm.Id);
            }
            else
            {
                Dispose();     // No one else is part of the game. 
                Logger.LogInformation("Game master left the game. No one else could be selected as new GM -> Disposing game...");
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
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0));
        
        Logger.LogTrace("Toggled game state to {state}.", State);
        return true;
    }
    
    public void ShufflePlayers()
    {
        ThrowWhenNotInit();
        _players = [.. _players.Shuffle()];
    }

    public void StartGame(GameOptions options)
    {
        ThrowWhenNotInit();
         if (State > GameState.Locked)
             throw new InvalidOperationException("Game game has already been started!");
         if (options.NightExecutionOrder.Distinct().Count() < options.NightExecutionOrder.Length)
             throw new ArgumentException($"{options.NightExecutionOrder} was expected to contain unique elements!", nameof(options));
         
         foreach (Player wwPlayer in _players.Shuffle().Take(options.AmountWerwolfs))     // Assign werwolfs first to ensure there is at least one.
             wwPlayer.Role = new Werwolf();
         
         RoleBase[] roles = [
             ..Enumerable.Repeat<RoleBase?>(null, options.AmountSeers).Select(_ => new Seer()),
             ..Enumerable.Repeat<RoleBase?>(null, options.AmountWitches).Select(_ => new Witch()),
             ..Enumerable.Repeat<RoleBase?>(null, options.AmountHunters).Select(_ => new Hunter()),
             ..options.AmorExists ? new RoleBase[] { new Amor() } : []
         ];
         roles = [..roles.Shuffle()];
         
         Player[] shuffledPlayers = [.._players.Where(p => p.Role is null).Shuffle()];
         
         int assignmentCount = Math.Min(roles.Length, shuffledPlayers.Length);
         for (var i = 0; i < assignmentCount; i++)
             shuffledPlayers[i].Role = roles[i];
         for (int i = assignmentCount; i < shuffledPlayers.Length; i++)
             shuffledPlayers[i].Role = new Villager();

         GameOptions = options;
         _gameLoopCts = new CancellationTokenSource();
         _gameLoop = RunAsync(_gameLoopCts.Token);
    }

    /// <summary>
    /// Starts a new action which requests from one or more players a selection.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="completedCallback">Gets invoked when the action finished (or was canceled).</param>
    /// <param name="ct">A cancellation token which cancels the whole action.</param>
    /// <returns>A tasks which waits until every player made a decision.</returns>
    internal async Task RequestPlayerActionAsync(PhaseAction action, Func<PhaseAction, CancellationToken, Task<string[]?>> completedCallback, CancellationToken ct)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using CancellationTokenRegistration ctr = ct.Register(_ => tcs.SetCanceled(ct), null);
        action.OnCompleted += _ => tcs.TrySetResult();
        
        RunningAction = action;
        OnPhaseAction?.Invoke(this, action);

        string[]? result = null;
        try
        {
            await tcs.Task;
            result = await completedCallback(action, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An exception occurred during a PhaseAction: {message}", ex.Message);
        }
        finally
        {
            OnPhaseActionCompleted?.Invoke(this, action, result);     // Frontend can (should) assume that every phase action is ended properly.
            RunningAction = null;
        }
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
        _disposed = true;
        _gameLoopCts?.Cancel();     // Don't dispose the task itself. It will throw
        State = GameState.NotInitialized;
    }
    
    private void ThrowWhenNotInit()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GameContext));
        if (State == GameState.NotInitialized)
            throw new InvalidOperationException("A game hasn't been initialized yet!");
    }
}