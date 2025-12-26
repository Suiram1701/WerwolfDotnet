namespace WerwolfDotnet.Server.Models;

public class JoinedGameDto
{
    /// <summary>
    /// The created player.
    /// </summary>
    public required PlayerDto Self { get; init; }
    
    /// <summary>
    /// The authentication token that can be used.
    /// A token used to authenticate as <see cref="Self"/> against the API.
    /// </summary>
    public required string PlayerToken { get; init; }
    /// </summary>
    public required string AuthenticationToken { get; init; }
}