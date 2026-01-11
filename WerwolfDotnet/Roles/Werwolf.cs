using System.Diagnostics;

namespace WerwolfDotnet.Roles;

[DebuggerDisplay($"{nameof(Name)}")]
public class Werwolf : IRole
{
    public string Name => nameof(Werwolf);
    
    public Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}