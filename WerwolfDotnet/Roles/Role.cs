namespace WerwolfDotnet.Roles;

/// <summary>
/// All existing roles. When the (int)role is greater than 0 it belongs to the villager and when smaller than 0 to the werwolfs.
/// </summary>
public enum Role
{
    WhiteWolf = -3,
    Urwolf = -2,
    Werwolf = -1,
    None = 0,
    Villager,
    Seer,
    SeerApprentice,
    Witch,
    Healer,
    Hunter,
    Amor,
    VillageMattress,
    TwoSisters,
    ThreeBrothers,
    BearGuide
}