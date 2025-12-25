using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Services.Interfaces;

namespace WerwolfDotnet.Server.Services;

public class InMemoryGameSessionStore : IGameSessionStore
{
    private readonly Dictionary<int, GameContext> _gameContexts = [];

    public Task<bool> IdExistsAsync(int gameId) =>
        Task.FromResult(_gameContexts.ContainsKey(gameId));
    
    public Task AddAsync(GameContext ctx)
    {
        return !_gameContexts.TryAdd(ctx.Id, ctx)
            ? throw new InvalidOperationException($"A game with the id {ctx.Id} already exists!")
            : Task.CompletedTask;
    }

    public Task<GameContext?> GetAsync(int id)
    {
        GameContext? ctx = _gameContexts.GetValueOrDefault(id);
        return Task.FromResult(ctx);
    }

    public Task<IEnumerable<GameContext>> GetAllAsync()
    {
        IEnumerable<GameContext> contexts = _gameContexts.Values;
        return Task.FromResult(contexts);
    }

    public Task UpdateAsync(GameContext ctx) => Task.CompletedTask;     // Not need to do anything because it will update its-self through references.

    public Task RemoveAsync(GameContext ctx)
    {
        _gameContexts.Remove(ctx.Id);
        return Task.CompletedTask;
    }
}