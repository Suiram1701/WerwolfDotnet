using WerwolfDotnet.Attributes;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet.Server.Models;

public class GameOptionsDto
{
    public Dictionary<int, int> AmountOfRoles { get; set; } = [];
    
    public bool ExplodingWitchHome { get; set; }
    
    public bool HunterMustKill { get; set; }
    
    public bool MayorDecidesNextMayor { get; set; }

    public CauseOfDeath[] RevealRoleForCauses { get; set; } = [];

    public GameOptions ToOptions()
    {
        return new GameOptions
        {
            AmountOfRoles = AmountOfRoles.ToDictionary(kvp => RoleAttribute.GetRoleType((Role)kvp.Key)!, kvp => kvp.Value),
            ExplodingWitchHome = ExplodingWitchHome,
            HunterMustKill = HunterMustKill,
            MayorDecidesNextMayor = MayorDecidesNextMayor,
            RevealRoleForCauses = RevealRoleForCauses
        };
    }
}