using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WerwolfDotnet.Attributes;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Options;

namespace WerwolfDotnet.Server.Controllers;

/// <summary>
/// Contains general endpoints used for various things.
/// </summary>
[ApiController]
[Route("api")]
public class RootController(IOptionsSnapshot<GameLobbyOptions> lobbyOptions, IOptionsSnapshot<Options.GameOptions> gameOptions) : ControllerBase
{
    private GameLobbyOptions LobbyOptions => lobbyOptions.Value;
    
    private Options.GameOptions GameOptions => gameOptions.Value;
    
    /// <summary>
    /// Retrieves configuration used by the client which also depends on the server.
    /// </summary>
    /// <returns>The client configuration.</returns>
    [HttpGet("config")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 7200)]     // Two hours
    [ProducesResponseType(typeof(ClientConfigDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetConfiguration()
    {
        return Ok(new ClientConfigDto
        {
            SessionsVisible = LobbyOptions.AllowViewAll,
            PlayerNameMinLength = LobbyOptions.PlayerNameMinLength,
            MinimumPlayers = LobbyOptions.MinPlayers,
            FixedRoleAmounts = RoleAttribute.GetRoles()
                .Where(attr => attr.FixedAmount != -1)
                .ToDictionary(attr => (int)attr.Role, attr => attr.FixedAmount),
            CanStartWhenNotReady = GameOptions.CanStartWhenNotReady,
            GameMasterSkipAllowed = GameOptions.GameMasterSkipAllowed
        });
    }
}