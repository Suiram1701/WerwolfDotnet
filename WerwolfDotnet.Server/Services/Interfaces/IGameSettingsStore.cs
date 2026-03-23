using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Services.Interfaces;

public interface IGameSettingsStore
{
    public Task UpdateAsync(int gameId, GameOptionsDto options);

    public Task<GameOptionsDto?> GetAsync(int gameId);
    
    public Task RemoveAsync(int gameId);
}