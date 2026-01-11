using System.Diagnostics;

namespace WerwolfDotnet.Roles;

[DebuggerDisplay($"{nameof(Name)}")]
public class Villager : IRole
{
    public string Name => nameof(Villager);
    
    public Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct) =>
        Task.CompletedTask;     // Nothing to do
}