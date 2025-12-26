namespace WerwolfDotnet.Server.Models;

public class JoinedGameDto
{
    /// <summary>
    /// The game created.
    /// </summary>
    public required GameDto Game { get; init; }
    
    /// <summary>
    /// The created player.
    /// </summary>
    public required PlayerDto Self { get; init; }
    
    /// <summary>
    /// A token used to authenticate as <see cref="Self"/> against the API.
    /// </summary>
    public required string PlayerToken { get; init; }

    /// <summary>
    /// The URL of SignalR WebSocket to connect to.
    /// </summary>
    public string SignalrUrl => $"{GameHubPath}?{GameSessionIdParam}={Game.Id}&{PlayerIdParam}={Self.Id}";
}