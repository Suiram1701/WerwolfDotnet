using WerwolfDotnet.Attributes;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet.Server.Models;

public class GameOptionsDto : ICloneable
{
    public Dictionary<int, int> AmountOfRoles { get; set; } = [];
    
    public bool SeerSeesRole { get; set; }
    
    public bool ExplodingWitchHome { get; set; }
    
    public bool HunterMustKill { get; set; }
    
    public bool MayorDecidesNextMayor { get; set; }

    public int DefaultVotingTime { get; init; } = 60;

    public int DiscussionTime { get; init; } = 300;

    public CauseOfDeath[] RevealRoleForCauses { get; set; } = [];
    

    public GameOptions ToOptions()
    {
        return new GameOptions
        {
            AmountOfRoles = AmountOfRoles.ToDictionary(kvp => RoleAttribute.GetRoleType((Role)kvp.Key)!, kvp => kvp.Value),
            SeerSeesRole = SeerSeesRole,
            ExplodingWitchHome = ExplodingWitchHome,
            HunterMustKill = HunterMustKill,
            MayorDecidesNextMayor = MayorDecidesNextMayor,
            RevealRoleForCauses = RevealRoleForCauses,
            // It's easier for the frontend to process the amount of seconds instead of .NETs TimeSpan format
            DefaultVotingTime = DefaultVotingTime != 0 ? TimeSpan.FromSeconds(DefaultVotingTime) : null,
            DiscussionTime = DiscussionTime != 0 ? TimeSpan.FromSeconds(DiscussionTime) : null
        };
    }

    public object Clone()
    {
        return new GameOptionsDto
        {
            AmountOfRoles = new Dictionary<int, int>(AmountOfRoles),
            SeerSeesRole = SeerSeesRole,
            ExplodingWitchHome = ExplodingWitchHome,
            HunterMustKill = HunterMustKill,
            MayorDecidesNextMayor = MayorDecidesNextMayor,
            RevealRoleForCauses = [..RevealRoleForCauses],
            DefaultVotingTime = DefaultVotingTime,
            DiscussionTime = DiscussionTime
        };
    }
}