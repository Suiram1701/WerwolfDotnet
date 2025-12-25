namespace WerwolfDotnet.Server.Models;

public class CreatedGameDto : JoinedGameDto
{
    /// <summary>
    /// The game created.
    /// </summary>
    public required GameDto Game { get; init; }
}