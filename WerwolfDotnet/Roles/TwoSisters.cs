namespace WerwolfDotnet.Roles;

[Role(Role.TwoSisters, FixedAmount = 2)]
public sealed class TwoSisters : RoleBase
{
    public override Role[] VisibleAllies => [Role.TwoSisters];
}