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
    /// Indicates whether new players can join the game.
    /// </summary>
    public bool CanJoin { get; } = context.State == GameState.Preparation && context.Players.Count < context.MaxPlayers;
    
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