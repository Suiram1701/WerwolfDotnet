namespace WerwolfDotnet;

public class GameOptions
{
    public int AmountWerwolfs { get; init; } = 1;

    public int AmountSeers { get; init; } = 1;
    
    public int AmountWitches { get; init; } = 1;

    public bool ExplodingWitchHome { get; init; } = false;
    
    public HashSet<CauseOfDeath> RevealRoleForCauses { get; init; } = [CauseOfDeath.WerwolfKilling, CauseOfDeath.WitchExplosion];
}