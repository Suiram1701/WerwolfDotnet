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
    
    private GameLobbyOptions LobbyOptions => lobbyOptions.CurrentValue;

    /// <summary>
    /// Checks whether the given player name is valid.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="context">If provided the availability in the current round is also checked.</param>
    /// <returns>true when valid.</returns>
    public bool IsPlayerNameValid(string name, GameContext? context)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (context is not null)
            return context.Players.All(p => p.Name != name);
        return true;
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
        return (player, authToken);
    }
}