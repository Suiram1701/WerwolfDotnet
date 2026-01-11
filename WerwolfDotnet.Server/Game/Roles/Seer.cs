using System.Diagnostics;

namespace WerwolfDotnet.Server.Game.Roles;

[DebuggerDisplay($"{nameof(Name)}")]
public class Seer : IRole
{
    public string Name => nameof(Seer);
    
    public Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}