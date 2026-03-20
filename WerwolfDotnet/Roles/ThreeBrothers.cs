namespace WerwolfDotnet.Roles;

[Role(Role.ThreeBrothers, FixedAmount = 3)]
public sealed class ThreeBrothers : RoleBase
{
    public override bool AlliesVisible => true;
}