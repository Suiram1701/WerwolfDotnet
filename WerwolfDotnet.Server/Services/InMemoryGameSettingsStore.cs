using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services.Interfaces;

namespace WerwolfDotnet.Server.Services;

public class InMemoryGameSettingsStore : IGameSettingsStore
{
    private readonly Dictionary<int, GameOptionsDto> _options = [];

    public Task UpdateAsync(int gameId, GameOptionsDto options)
    {
        _options[gameId] = options;
        return Task.CompletedTask;
    }

    public Task<GameOptionsDto?> GetAsync(int gameId)
    {
        GameOptionsDto? options = _options.GetValueOrDefault(gameId);
        return Task.FromResult(options);
    }

    public Task RemoveAsync(int gameId)
    {
        _options.Remove(gameId);
        return Task.CompletedTask;
    }
}