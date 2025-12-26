using WerwolfDotnet.Server.Game;

namespace WerwolfDotnet.Server.Models;

/// <summary>
/// A Dto for returning a game.
/// </summary>
public class GameDto(GameContext context)
{
    /// <summary>
    /// The identifier of the game
    /// </summary>
    public int Id { get; } = context.Id;

    /// <summary>
    /// Indicates whether the game is password protected.
    /// </summary>
    public bool Protected { get; } = context.IsProtected;

    /// <summary>
    /// Indicates whether the game is currently running. You can't join ongoing games.
    /// </summary>
    public bool IsRunning { get; } = context.State > GameState.Preparation;

    /// <summary>
    /// The name of the game master (the one who is hosting the game)
    /// </summary>
    public string GameMaster { get; } = context.GameMaster.Name;
    
    /// <summary>
    /// The current amount of players in the game.
    /// </summary>
    public int PlayerCount { get; } = context.Players.Count;

    /// <summary>
    /// The maximum amount of player in a game.
    /// </summary>
    public int MaxPlayerCount { get; } = context.MaxPlayers;
}