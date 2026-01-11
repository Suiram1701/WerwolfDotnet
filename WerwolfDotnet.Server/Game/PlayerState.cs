namespace WerwolfDotnet.Server.Game;

/// <summary>
/// Represents the status a player is in.
/// </summary>
public enum PlayerState
{
    /// <summary>
    /// The player is normally alive.
    /// </summary>
    Alive = 0,
    
    /// <summary>
    /// The player was selected by someone (werwolf, witch, ...) to die.
    /// </summary>
    PendingDeath = 1,
    
    /// <summary>
    /// The player is completely death for this round.
    /// </summary>
    Death = 2
}