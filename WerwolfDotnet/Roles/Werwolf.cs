namespace WerwolfDotnet.Roles;

public sealed class Werwolf : RoleBase
{
    public override Role Type => Role.Werwolf;
    
    // The Werwolf action is handled separately in GameContext._RunNightAsync(CancellationToken) because it's a multi-player action.
}