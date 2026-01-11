namespace WerwolfDotnet;

/// <summary>
/// Different states the game can be in.
/// </summary>
public enum GameState
{
    /// <summary>
    /// The game hasn't been set up internally.
    /// </summary>
    NotInitialized = -2,
    
    /// <summary>
    /// The game isn't running and roles weren't distributed. Wait for the game master to start.
    /// </summary>
    Preparation = -1,
    
    /// <summary>
    /// The game is locked and no one can join.
    /// </summary>
    Locked = 0,
    
    /// <summary>
    /// Its is currently day time and villager can vote other players.
    /// </summary>
    Day = 1,
    
    /// <summary>
    /// Its night. No one (except the Werwolfs) are allowed to talk.
    /// </summary>
    Night = 2,
}