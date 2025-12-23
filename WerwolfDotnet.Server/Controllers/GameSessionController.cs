using Microsoft.AspNetCore.Mvc;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Controllers;

[ApiController]
[Route("api/v1/gamesessions")]
public class GameSessionController(GameManager manager) : ControllerBase
{
    private readonly GameManager _manager = manager;
    
    /// <summary>
    /// Retrieves all existing game sessions. This endpoint can be disabled by the server.
    /// </summary>
    /// <returns>A list of all game sessions.</returns>
    /// <response code="200">All game sessions.</response>
    /// <response code="403">Returning all sessions is disabled by the server.</response>
    [HttpGet]
    public async Task<IActionResult> GetAllSessions()
    {
        IEnumerable<GameContext>? contexts = await _manager.GetAllGames().ConfigureAwait(false);
        if (contexts is null)
            return Forbid();
        return Ok(contexts);
    }

    /// <summary>
    /// Retrieves a single game session by its id.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <returns>The session</returns>
    /// <response code="200">The game corresponding to the provided id.</response>
    /// <response code="404">No game with the id could be found</response>
    [HttpGet("{sessionId:int}")]
    public async Task<IActionResult> GetSessionById(int sessionId)
    {
        GameContext? ctx = await _manager.GetGameById(sessionId).ConfigureAwait(false);
        return ctx is not null
            ? Ok(new GameDto(ctx))
            :  NotFound();
    }
}