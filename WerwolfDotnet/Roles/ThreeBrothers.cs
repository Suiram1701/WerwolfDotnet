namespace WerwolfDotnet.Roles;

public sealed class ThreeBrothers : RoleBase
{
    public override Role Type => Role.ThreeBrothers;

    public override bool AlliesVisible => true;
}