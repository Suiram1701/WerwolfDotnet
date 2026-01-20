namespace WerwolfDotnet.Roles;

public sealed class Witch : IRole
{
    public Role Type => Role.Witch;

    public bool CanHeal { get; internal set; } = true;

    public bool CanKill { get; internal set; } = true;
}