namespace WerwolfDotnet;

/// <summary>
/// All existing roles. When the (int)role is greater than 0 it belongs to the villager and when smaller than 0 to the werwolfs.
/// </summary>
public enum Role
{
    Werwolf = -1,
    None = 0,
    Villager,
    Seer,
    Witch,
    Hunter,
    Amor
}