namespace WerwolfDotnet.Server.Game;

/// <summary>
/// Different states the game can be in.
/// </summary>
public enum GameState
{
    /// <summary>
    /// The game isn't running and roles weren't distributed. Wait for the game master to start.
    /// </summary>
    Preparation
}