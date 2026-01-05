using Microsoft.AspNetCore.Mvc;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;
using static System.Net.Mime.MediaTypeNames;

namespace WerwolfDotnet.Server.Controllers;

[ApiController]
[Route("api/game_sessions")]
public class GameSessionController(GameManager manager) : ControllerBase
{
    private readonly GameManager _manager = manager;

    /// <summary>
    /// Creates a new game and the game master who created it.
    /// </summary>
    /// <param name="model">The Dto for creating the game.</param>
    /// <response code="201">A new game was created.</response>
    /// <response code="400">Invalid values were provided.</response>
    [HttpPost]
    [ProducesResponseType(typeof(JoinedGameDto), StatusCodes.Status201Created, Application.Json)]
    public async Task<IActionResult> CreateSession([FromBody] JoinGameDto model)
    {
        if (!_manager.IsPlayerNameValid(model.PlayerName.Trim(), null))
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "A valid 'playerName' was expected!");
        
        (GameContext ctx, Player self, string auth) = await _manager.CreateGameAsync(model.PlayerName.Trim(), model.GamePassword);
        return CreatedAtAction(nameof(GetSessionById), new { sessionId = ctx.Id }, new JoinedGameDto
        {
            Game = new GameDto(ctx),
            Self = new PlayerDto(self),
            PlayerToken = auth
        });
    }

    /// <summary>
    /// Retrieves all existing game sessions. This endpoint can be disabled by the server.
    /// </summary>
    /// <returns>A list of all game sessions.</returns>
    /// <response code="200">All game sessions.</response>
    /// <response code="403">Returning all sessions is disabled by the server.</response>
    [HttpGet]
    [ProducesResponseType(typeof(GameDto[]), StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> GetAllSessions()
     {
        IEnumerable<GameContext>? contexts = await _manager.GetAllGames().ConfigureAwait(false);
        if (contexts is null)
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Configuration disables viewing all sessions!");
        return Ok(contexts
            .Where(ctx => ctx.State > GameState.NotInitialized)
            .Select(ctx => new GameDto(ctx)));
    }

    /// <summary>
    /// Retrieves a single game session by its id.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <returns>The session</returns>
    /// <response code="200">The game corresponding to the provided id.</response>
    /// <response code="404">No game with the id could be found</response>
    [HttpGet("{sessionId:int}")]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> GetSessionById(int sessionId)
    {
        GameContext? ctx = await _manager.GetGameById(sessionId).ConfigureAwait(false);
        if (ctx?.State == GameState.NotInitialized)
            ctx = null;
        return ctx is not null
            ? Ok(new GameDto(ctx))
            : Problem(statusCode: StatusCodes.Status404NotFound, detail: "Session not found.");
    }

    /// <summary>
    /// Creates a new player in an existing game session.
    /// </summary>
    /// <param name="sessionId">The id of the session to join.</param>
    /// <param name="model">The Dto containing necessary data.</param>
    /// <returns>The created player and corresponding data.</returns>
    /// <response code="200">The created player.</response>
    /// <response code="400">The player name is already taken or invalid.</response>
    /// <response code="401">The provided session password is wrong.</response>
    /// <response code="404">No session with the provided id found.</response>
    /// <response code="409">Game already full.</response>
    [HttpPost("{sessionId:int}/join")]
    [ProducesResponseType(typeof(JoinedGameDto), StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> JoinPlayerAsync([FromRoute] int sessionId, [FromBody] JoinGameDto model)
    {
        GameContext? ctx = await _manager.GetGameById(sessionId).ConfigureAwait(false);
        if (ctx is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Specified session not found.");

        if (!_manager.IsPlayerNameValid(model.PlayerName.Trim(), ctx))
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "The provided 'playerName' is invalid or already taken.");

        if (ctx.State != GameState.Preparation)
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "The game is already running or locked.");
        if (ctx.Players.Count >= ctx.MaxPlayers)
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "The session is already full!");
        
        (Player self, string authToken)? playerData = await _manager.JoinGameAsync(ctx, model.PlayerName.Trim(), model.GamePassword);
        if (playerData is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The provided password is invalid.");
        
        return Ok(new JoinedGameDto
        {
            Game = new GameDto(ctx),
            Self = new PlayerDto(playerData.Value.self),
            PlayerToken = playerData.Value.authToken
        });
    }
}