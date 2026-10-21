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
    Villager = 1,
    Seer = 2,
    SeerApprentice = 3,
    Witch = 4,
    Healer = 5,
    Hunter = 6,
    Amor = 7,
    VillageMattress = 8,
    TwoSisters = 9,
    ThreeBrothers = 10,
    BearGuide = 11
}