namespace WerwolfDotnet.Server.Models;

public class JoinedGameDto
{
    /// <summary>
    /// The created player.
    /// </summary>
    public required PlayerDto Self { get; init; }
    
    /// <summary>
    /// The authentication token that can be used.
    /// </summary>
    public required string AuthenticationToken { get; init; }
}