using WerwolfDotnet.Server.Game;

namespace WerwolfDotnet.Server.Services.Interfaces;

/// <summary>
/// An interface contract for storing whole games.
/// </summary>
public interface IGameSessionStore
{
    /// <summary>
    /// Checks whether a game with a specific Id already exists
    /// </summary>
    /// <remarks>
    /// -> checks whether the id has to be regenerated.
    /// </remarks>
    /// <param name="gameId">The game id to check.</param>
    /// <returns><c>true</c> when the ID exists.</returns>
    public Task<bool> IdExistsAsync(int gameId);
    
    /// <summary>
    /// Stores a new game context.
    /// </summary>
    /// <param name="ctx">The context to store.</param>
    /// <returns>A task to await in case a remote server is involved.</returns>
    public Task AddAsync(GameContext ctx);

    /// <summary>
    /// Retrieves a game session by its ID.
    /// </summary>
    /// <param name="id">The id of the game.</param>
    /// <returns>The game context. <c>null</c> when not context is associated with this ID.</returns>
    public Task<GameContext?> GetAsync(int id);

    /// <summary>
    /// Gets all existing game contexts.
    /// </summary>
    /// <remarks>
    /// Is optional in case this feature is disabled by server config.
    /// </remarks>
    /// <returns>An enumerable of all servers.</returns>
    public Task<IEnumerable<GameContext>> GetAllAsync();
    
    /// <summary>
    /// Updates the stored context after changes were made.
    /// </summary>
    /// <param name="ctx">The new context to store.</param>
    /// <returns>A task to await in case a remote server is involved.</returns>
    public Task UpdateAsync(GameContext ctx);
    
    /// <summary>
    /// Removes a session from store. It still exists but isn't persisted.
    /// </summary>
    /// <param name="ctx">The context to remove</param>
    /// <returns>A task to await in case a remote server is involved.</returns>
    public Task RemoveAsync(GameContext ctx);
}