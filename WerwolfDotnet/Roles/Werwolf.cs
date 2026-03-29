namespace WerwolfDotnet.Roles;

[Role(Role.Werwolf)]
public class Werwolf : RoleBase
{
    public sealed override Role[] VisibleAllies => [Role.Werwolf, Role.Urwolf, Role.WhiteWolf];

    // The Werwolf action is handled separately in GameContext._RunNightAsync(CancellationToken) because it's a multi-player action.
}