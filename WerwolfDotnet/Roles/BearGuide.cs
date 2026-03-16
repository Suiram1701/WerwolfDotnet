namespace WerwolfDotnet.Roles;

public sealed class BearGuide : RoleBase
{
    public override Role Type => Role.BearGuide;
    
    // Logic handled in GameContext.GameLoop.cs
}