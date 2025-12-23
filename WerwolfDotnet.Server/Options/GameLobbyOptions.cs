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
}