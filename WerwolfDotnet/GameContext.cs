using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WerwolfDotnet.Logging;
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

    public IEnumerable<PhaseAction> PreviousActions => _previousActions;
    private readonly Stack<PhaseAction> _previousActions = [];
    
    /// <summary>
    /// The logger used for the entire session.
    /// </summary>
    public GameLogger Logger { get; }
    
    /// <summary>
    /// Shows when this round was started. When a game finishes and a new one starts this will be updated.
    /// </summary>
    public DateTimeOffset RoundStartedAt { get; private set; }

    /// <summary>
    /// First int is the game master id and seconds integer is the id of the village mayor.
    /// </summary>
    public event Action<GameContext, int, int?>? OnGameMetadataChanged;
    
    /// <summary>
    /// Invoked when the state of the game changed. (Player, CauseOfDeath) are the players who died during the previous state mapped to the cause of death,
    /// the last one indicates whether the bear growls or not (when <c>null</c> it shouldn't be displayed).
    /// </summary>
    public event Action<GameContext, GameState, IReadOnlyDictionary<Player, (CauseOfDeath, Role)>, bool?>? OnGameStateChanged;
    
    /// <summary>
    /// Invoked when one or more players are requested to take action. 
    /// </summary> 
    public event Action<GameContext, PhaseAction>? OnPhaseAction;

    /// <summary>
    /// Invoked when a startet phase action completed. The last param are parameters passed for displaying a result.
    /// When <c>null</c> nothing should be shown.
    /// </summary>
    public event Action<GameContext, PhaseAction, string[]?>? OnPhaseActionCompleted;

    /// <summary>
    /// Invoked when one of the fractions has won the game. First bool is whether the village won the game.
    /// </summary>
    public event Action<GameContext, Fraction>? OnGameWon;
    
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
    public GameContext(int id, string? password, int maxPlayers, GameLogger logger)     // Makes constructor private
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
        Logger.Log(Event.Joined, gameMaster);
        State = GameState.Preparation;
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0), null);
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
        if (State > 0)
            throw new InvalidOperationException("Game game has already been started!");
        _players.Add(player);
        Logger.Log(Event.Joined, player);
    }

    public bool RemovePlayer(Player player)
    {
        ThrowWhenNotInit();
        
        if (!_players.Remove(player))
            return false;
        Logger.Log(Event.Left, player);

        if (!player.Equals(GameMaster))
            return true;
        
        Player? newGm = _players.MinBy(p => p.Id);     // Elected by the id -> the one who joined after the GM
        if (newGm is not null)
        {
            GameMaster = newGm;
            Logger.Log(Event.BecameGameMaster, source: player, target: newGm); 
            OnGameMetadataChanged?.Invoke(this, newGm.Id, Mayor?.Id);
        }
        else
        {
            Dispose();     // No one else is part of the game. 
        }
        
        return true;
    }

    public bool SetJoinLock(bool @lock)
    {
        ThrowWhenNotInit();

        if (State > 0)     // Game is already running
            return false;
        State = @lock
            ? GameState.Locked
            : GameState.Preparation;
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0), null);
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
         if (State > 0)
             throw new InvalidOperationException("Game game has already been started!");
         if (options.AmountOfRoles.Keys.Any(t => !t.IsAssignableTo(typeof(RoleBase)) || t.GetConstructor(Type.EmptyTypes) is null))
             throw new ArgumentException($"Every role type specified in {nameof(options.AmountOfRoles)} must inherit from {nameof(RoleBase)} and have a public parameterless constructor.", nameof(options));
         if (options.NightExecutionOrder.Distinct().Count() < options.NightExecutionOrder.Length)
             throw new ArgumentException($"{options.NightExecutionOrder} was expected to contain unique elements!", nameof(options));

         if (!options.AmountOfRoles.TryGetValue(typeof(Werwolf), out int value) || value < 1)
             throw new ArgumentException($"{nameof(options.AmountOfRoles)} at least have to contain {nameof(Werwolf)} with a minimum of 1", nameof(options));
         foreach (Player wwPlayer in _players.Shuffle().Take(options.AmountOfRoles[typeof(Werwolf)]))     // Assign werwolfs first to ensure there is at least one.
             wwPlayer.Role = new Werwolf();
         
         RoleBase[] roles = options.AmountOfRoles
             .Where(kvp => kvp.Key != typeof(Werwolf))
             .SelectMany(kvp =>
                 Enumerable.Repeat<RoleBase>(null!, kvp.Value)
                     .Select(_ => (RoleBase)Activator.CreateInstance(kvp.Key)!))
             .Shuffle()
             .ToArray();
         
         Player[] shuffledPlayers = [.._players.Where(p => p.Role is null).Shuffle()];
         
         int assignmentCount = Math.Min(roles.Length, shuffledPlayers.Length);
         for (var i = 0; i < assignmentCount; i++)
             shuffledPlayers[i].Role = roles[i];
         for (int i = assignmentCount; i < shuffledPlayers.Length; i++)
             shuffledPlayers[i].Role = new Villager();

         GameOptions = options;
         _gameLoopCts = new CancellationTokenSource();
         _gameLoop = RunAsync(_gameLoopCts.Token);
         RoundStartedAt = DateTimeOffset.UtcNow;
         Logger.Log(Event.GameStarted);
    }

    public void StopGame()
    {
        ThrowWhenNotInit();
        if (State <= 0)
            return;
        
        _gameLoopCts?.Cancel();

        RunningAction = null;
        _previousActions.Clear();

        foreach (Player player in _players)
            player.Reset();
        Round = 0;
        Mayor = null;
        _playersInLove.Clear();
        _werwolfProtectedPlayers.Clear();
        
        State = GameState.Preparation;
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0), null);
        Logger.Log(Event.GameStopped);
        
        _gameLoopCts = null;
        _gameLoop = null;
        GameOptions = null;
    }

    /// <summary>
    /// Starts a new action which requests from one or more players a selection.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="completedCallback">Gets invoked when the action finished (or was canceled).</param>
    /// <returns>A tasks which waits until every player made a decision.</returns>
    internal async Task RequestPlayerActionAsync(PhaseAction action, Func<PhaseAction, CancellationToken, Task<string[]?>> completedCallback)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using CancellationTokenRegistration ctr = action.CancellationToken.Register(_ => tcs.SetCanceled(action.CancellationToken), null);
        action.OnCompleted += (_, _) => tcs.TrySetResult();
        
        RunningAction = action;
        OnPhaseAction?.Invoke(this, action);
        
        try
        {
            await tcs.Task;
        }
        catch (TaskCanceledException ex)
        {
            // TODO: What to do here?
            // Logger.LogWarning(ex, "PhaseAction {type} was cancelled due to timeout/stop", action.Type);
            action.OrgCancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            string[]? result = null;
            try
            {
                Logger.Log(Event.Voting, args: [action.Type, ..action.PlayerVotes.ToArray()]);
                result = await completedCallback(action, action.CancellationToken);
            }
            finally
            {
                // Frontend can (should) assume that every phase action is ended properly.
                OnPhaseActionCompleted?.Invoke(this, action, result);
                _previousActions.Push(action);
                RunningAction = null;
            }
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
        
        foreach (Player player in _players)
            player.Reset();
        State = GameState.NotInitialized;
        
        _gameLoopCts = null;
        _gameLoop = null;
        GameOptions = null;
    }
    
    private void ThrowWhenNotInit()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GameContext));
        if (State == GameState.NotInitialized)
            throw new InvalidOperationException("A game hasn't been initialized yet!");
    }
}