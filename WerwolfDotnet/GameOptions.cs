namespace WerwolfDotnet;

public class GameOptions
{
    public int AmountWerwolfs { get; init; } = 1;

    public int AmountSeers { get; init; } = 1;
    
    public int AmountWitches { get; init; } = 1;

    public bool ExplodingWitchHome { get; init; } = false;
    
    public int AmountHunters { get; init; } = 1;
    
    public bool HunterMustKill { get; init; } = false;
    
    public Role[] NightExecutionOrder { get; init; } = [Role.Werwolf, Role.Seer, Role.Witch];
    
    public CauseOfDeath[] RevealRoleForCauses { get; init; } = [CauseOfDeath.WerwolfKilling, CauseOfDeath.WitchExplosion];
}