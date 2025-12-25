namespace WerwolfDotnet.Server.Models;

/// <summary>
/// A Dto for creating or joining a gme.
/// </summary>
public class JoinGameDto
{
    /// <summary>
    /// The name of the name to add.
    /// </summary>
    public string PlayerName { get; set; } = null!;
    
    /// <summary>
    /// An optional password to protect the session with.
    /// </summary>
    public string? SessionPassword { get; set; }
}