using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Options;

public class GameOptions
{
    /// <summary>
    /// Indicates whether the server is allowed to accept an action skip by the game master.
    /// </summary>
    public bool GameMasterSkipAllowed { get; set; } = true;

    /// <summary>
    /// The options used for a newly created instance
    /// </summary>
    public GameOptionsDto DefaultOptions { get; set; } = new();
}