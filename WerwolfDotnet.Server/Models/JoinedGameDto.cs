using System.Text.Json;
using System.Text.Json.Serialization;
using WerwolfDotnet.Server.Authentication;

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
    /// The internally used authentication secret.
    /// </summary>
    [JsonIgnore]
    public string? PlayerToken { get; init; }     // Can't use 'required' because JSON Lib doesn't like it.

    /// <summary>
    /// The base64 encoded JSON object build out of the game id, player id and <see cref="PlayerToken"/>
    /// </summary>
    public string BearerToken => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
        new TokenAuthenticationScheme.Token(Game.Id, Self.Id, PlayerToken ?? throw new InvalidOperationException("PlayerToken required!"))));

    /// <summary>
    /// The URL of SignalR WebSocket to connect to.
    /// </summary>
    public string SignalrUrl => GameHubPath;
}