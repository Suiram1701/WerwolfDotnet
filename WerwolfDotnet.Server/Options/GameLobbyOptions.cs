using System.ComponentModel.DataAnnotations;

namespace WerwolfDotnet.Server.Options;

/// <summary>
/// Options to configure the behavior in the game lobby
/// </summary>
public class GameLobbyOptions
{
    /// <summary>
    /// Indicates whether everyone can see every session. Default is true
    /// </summary>
    public bool AllowViewAll { get; set; } = true;
    
    /// <summary>
    /// The maximum amount of players per session.
    /// Values from 2 to 128 allowed. Default is 32
    /// </summary>
    [Range(2, 128)]
    public int MaxPlayers { get; set; } = 32;
    
    /// <summary>
    /// The minimum length of a player's name
    /// </summary>
    [Range(1, 255)]
    public int PlayerNameMinLength { get; set; } = 3;

    /// <summary>
    /// The maximum length of a player's name
    /// </summary>
    [Range(2, 256)]
    public int PlayerNameMaxLength { get; set; } = 16;

    /// <summary>
    /// Indicates whether a player name can contain numbers.
    /// </summary>
    public bool PlayerNameAllowNumbers { get; set; } = true;

    /// <summary>
    /// Words which can't be in a player name (case-insensitive)
    /// </summary>
    public List<string> PlayerNameForbiddenWords { get; set; } = [];
}