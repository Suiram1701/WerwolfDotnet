using WerwolfDotnet.Server.Game;

namespace WerwolfDotnet.Server.Services.Interfaces;

public interface IGameSessionStore
{
    public Task AddAsync(GameContext ctx);

    public Task<GameContext?> GetAsync(int id);

    public Task<IEnumerable<GameContext>> GetAllAsync();
    
    public Task RemoveAsync(GameContext ctx);
}