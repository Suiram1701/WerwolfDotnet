namespace WerwolfDotnet.Roles;

public sealed class TwoSisters : RoleBase
{
    public override Role Type => Role.TwoSisters;

    public override bool AlliesVisible => true;
}