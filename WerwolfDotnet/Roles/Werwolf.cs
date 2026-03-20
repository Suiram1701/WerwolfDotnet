namespace WerwolfDotnet.Roles;

[Role(Role.Werwolf)]
public sealed class Werwolf : RoleBase
{
    public override bool AlliesVisible => true;

    // The Werwolf action is handled separately in GameContext._RunNightAsync(CancellationToken) because it's a multi-player action.
}