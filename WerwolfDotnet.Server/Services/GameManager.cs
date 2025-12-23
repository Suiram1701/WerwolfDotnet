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
}