using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Options;

public class GameOptions
{
    /// <summary>
    /// When <c>true</c> it's allowed to start the game when not all players are ready. Otherwise, the game cannot be startet.
    /// </summary>
    public bool CanStartWhenNotReady { get; set; } = true;
    
    /// <summary>
    /// Indicates whether the server is allowed to accept an action skip by the game master.
    /// </summary>
    public bool GameMasterSkipAllowed { get; set; } = true;

    /// <summary>
    /// The options used for a newly created instance
    /// </summary>
    public GameOptionsDto DefaultOptions { get; set; } = new();
}