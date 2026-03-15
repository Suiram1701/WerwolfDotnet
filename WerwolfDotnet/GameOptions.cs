using System.Collections.ObjectModel;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet;

public class GameOptions
{
    public IReadOnlyDictionary<Type, int> AmountOfRoles { get; init; } = new Dictionary<Type, int>
    {
        { typeof(Werwolf), 1 },
        { typeof(Seer), 1 },
        { typeof(SeerApprentice), 0 },
        { typeof(Witch), 1 },
        { typeof(Hunter), 0 },
        { typeof(Amor), 1 },
        { typeof(VillageMattress), 0 },
        { typeof(TwoSisters), 0 },     // Has to be tested
        { typeof(ThreeBrothers), 0 }
    }.AsReadOnly();

    public bool ExplodingWitchHome { get; init; } = false;
    
    public bool HunterMustKill { get; init; } = false;
    
    public bool MayorDecidesNextMayor { get; init; } = true;
    
    public Role[] NightExecutionOrder { get; init; } = [Role.Amor, Role.VillageMattress, Role.Werwolf, Role.Seer, Role.SeerApprentice, Role.Witch];
    
    public CauseOfDeath[] RevealRoleForCauses { get; init; } = [CauseOfDeath.WerwolfKilling, CauseOfDeath.WitchExplosion];
}