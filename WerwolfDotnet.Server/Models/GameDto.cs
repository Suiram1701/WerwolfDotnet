using WerwolfDotnet.Server.Game;

namespace WerwolfDotnet.Server.Models;

/// <summary>
/// An Dto for returning a game.
/// </summary>
public class GameDto
{
    /// <summary>
    /// The identifier of the game
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// Indicates whether the game is password protected.
    /// </summary>
    public bool Protected { get; }
    
    /// <summary>
    /// Indicates whether the game is currently running. You can't join ongoing games.
    /// </summary>
    public bool IsRunning { get; }
    
    /// <summary>
    /// The player ID of the game master (the one who is hosting the game)
    /// </summary>
    public int GameMasterId  { get; }
    
    /// <summary>
    /// A list of all players and their name who are participating in the game.
    /// </summary>
    public PlayerDto[] Players { get; }

    /// <summary>
    /// Converts a game context into this Dto
    /// </summary>
    /// <param name="ctx">The context to create from.</param>
    public GameDto(GameContext ctx)
    {
        Id = ctx.Id;
        Protected = ctx.IsProtected;
        IsRunning = ctx.State != GameState.Preparation;
        // GameMasterId = 
        // Players = 
    }
}