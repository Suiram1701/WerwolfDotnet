using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Options;
using WerwolfDotnet.Server.Services.Interfaces;

namespace WerwolfDotnet.Server.Services;

public class GameManager(ILogger<GameManager> logger, ILoggerFactory loggerFactory, IGameSessionStore sessionStore, IOptionsMonitor<GameLobbyOptions> lobbyOptions)
{
    private readonly ILogger<GameManager> _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IGameSessionStore _sessionStore = sessionStore;

    /// <summary>
    /// Gets invoked when a player was added, removed or the order was changed.
    /// </summary>
    public event Func<GameContext, IEnumerable<Player>, Task> OnPlayersUpdated
    {
        add => _playersChanged.Add(value);
        remove => _playersChanged.Remove(value);
    }
    private readonly List<Func<GameContext, IEnumerable<Player>, Task>> _playersChanged = [];
    
    /// <summary>
    /// Gets invoked when the game master (second param) or the mayor of the village (third param) changed.
    /// </summary>
    public event Func<GameContext, Player, Player?, Task> OnGameMetaUpdated
    {
        add => _gameMetaUpdated.Add(value);
        remove => _gameMetaUpdated.Remove(value);
    }
    private readonly List<Func<GameContext, Player, Player?, Task>> _gameMetaUpdated = [];
    
    public event Func<GameContext, GameState, Task> OnGameStateUpdated
    {
        add => _gameStateUpdated.Add(value);
        remove => _gameStateUpdated.Remove(value);
    }
    private readonly List<Func<GameContext, GameState, Task>> _gameStateUpdated = [];
    
    private GameLobbyOptions LobbyOptions => lobbyOptions.CurrentValue;

    /// <summary>
    /// Checks whether the given player name is valid.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="context">If provided the availability in the current round is also checked.</param>
    /// <returns>true when valid.</returns>
    public bool IsPlayerNameValid(string name, GameContext? context)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace))
            return false;
        if (name.Length < LobbyOptions.PlayerNameMinLength || name.Length > LobbyOptions.PlayerNameMaxLength)
            return false;
        
        string pattern = LobbyOptions.PlayerNameAllowNumbers
            ? "^[a-zA-Z0-9]+$"
            : "^[a-zA-Z]+$";
        if (!Regex.IsMatch(name, pattern))
            return false;

        if (LobbyOptions.PlayerNameForbiddenWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase)))
            return false;
        
        return context is null || context.Players.All(p => p.Name != name);
    }
    
    /// <summary>
    /// Creates a new game session and the corresponding game master.
    /// </summary>
    /// <param name="gameMasterName">The display name of the game master</param>
    /// <param name="sessionPassword">An optional session password to set.</param>
    /// <returns>The game <c>context</c> of the created game, the placer instance of the <c>gameMaster</c> and the <c>gameMasterAuth</c>-token of the game master.</returns>
    public async Task<(GameContext context, Player gameMaster, string gameMasterAuth)> CreateGameAsync(string gameMasterName, string? sessionPassword)
    {
        int gameId;
        do
        {
            gameId = Random.Shared.Next(0, 999999);
        } while (await _sessionStore.IdExistsAsync(gameId));
        ILogger gameLogger = _loggerFactory.CreateLogger($"{typeof(GameContext)}[{gameId}]");
        
        GameContext context = new(gameId, sessionPassword, LobbyOptions.MaxPlayers, gameLogger);
        Player gameMaster = new(0, gameMasterName, context, out string gameMasterAuth);
        context.InitializeGame(gameMaster);

        await _sessionStore.AddAsync(context);
        _logger.LogInformation("Game {gameId} created. Game master is {gameMasterName} ({gameMasterId})", gameId, gameMasterName, 0);
        
        return (context, gameMaster, gameMasterAuth);
    }
    
    /// <summary>
    /// Retrieves a game by its ID.
    /// </summary>
    /// <param name="id">The game ID</param>
    /// <returns>The game. When not found <c>null</c></returns>
    public async Task<GameContext?> GetGameById(int id)
    {
        return await _sessionStore.GetAsync(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all game sessions.
    /// </summary>
    /// <returns>A list of all sessions. When not permitted by server setting <c>null</c> is returned.</returns>
    public async Task<IEnumerable<GameContext>?> GetAllGames()
    {
        if (!LobbyOptions.AllowViewAll)
            return null;
        return await _sessionStore.GetAllAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a player to an existing game.
    /// </summary>
    /// <param name="ctx">The game to add the player to.</param>
    /// <param name="playerName">The name of the player to add.</param>
    /// <param name="password">A password to provide when the game session is protected.</param>
    /// <returns>The created player (<c>self</c>) and the corresponding <c>authToken</c>. <c>null</c> when the password is invalid or the maximum amount of players is reached.</returns>
    public async Task<(Player self, string authToken)?> JoinGameAsync(GameContext ctx, string playerName, string? password)
    {
        if (!ctx.VerifyPassword(password))
            return null;

        if (ctx.Players.Count >= ctx.MaxPlayers)
            return null;

        Player player = new(ctx.Players.Count, playerName, ctx, out string authToken);
        ctx.AddPlayer(player);

        await _sessionStore.UpdateAsync(ctx);     // No need to log (done by context)
        await InvokeAsyncEvent(_playersChanged, cb => cb.Invoke(ctx, ctx.Players)).ConfigureAwait(false);
        return (player, authToken);
    }

    /// <summary>
    /// Removes a player from a game. Can be used when a player left on his own will or when he was kicked.
    /// </summary>
    /// <param name="ctx">The game the player is a part of.</param>
    /// <param name="player">The player to remove.</param>
    /// <returns>Indicates whether it was successful.</returns>
    public async Task<bool> LeaveGameAsync(GameContext ctx, Player player)
    {
        bool isGameMaster = player.Equals(ctx.GameMaster);
        if (!ctx.RemovePlayer(player))
            return false;

        if (ctx.Players.Count > 0)
        {
            await _sessionStore.UpdateAsync(ctx).ConfigureAwait(false);

            if (isGameMaster)
                await InvokeAsyncEvent(_gameMetaUpdated, cb => cb.Invoke(ctx, ctx.GameMaster, ctx.Mayor));
            await InvokeAsyncEvent(_playersChanged, cb => cb.Invoke(ctx, ctx.Players)).ConfigureAwait(false);
            return true;
        }
        
        
        
        // empty session -> auto remove
        ctx.Dispose();
        await _sessionStore.RemoveAsync(ctx).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ToggleGameLockedAsync(GameContext ctx)
    {
        if (ctx.ToggleJoinLock())
        {
            await _sessionStore.UpdateAsync(ctx).ConfigureAwait(false);
            await InvokeAsyncEvent(_gameStateUpdated, cb => cb.Invoke(ctx, ctx.State)).ConfigureAwait(false);
            return true;
        }
        return false;
    }
    
    private static Task InvokeAsyncEvent<T>(IEnumerable<T> callbacks, Func<T, Task> invoke)
    {
        IEnumerable<Task> tasks = callbacks.Select(invoke);
        return Task.WhenAll(tasks);
    }
}