using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Attributes;
using WerwolfDotnet.Roles;
using WerwolfDotnet.Server.Hubs;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;
using WerwolfDotnet.Server.Services.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace WerwolfDotnet.Server.Controllers;

/// <summary>
/// Contains game session related endpoints.
/// </summary>
[ApiController]
[Route("api/game_sessions")]
public class GameSessionController(
    ILogger<GameSessionController> logger,
    GameManager manager,
    IGameSettingsStore settingsStore,
    IHubContext<GameHub, IGameHub> hubContext,
    PlayerConnectionMapper connectionMapping) : ControllerBase
{
    private readonly ILogger _logger = logger;
    private readonly GameManager _manager = manager;
    private readonly IGameSettingsStore _settingsStore = settingsStore;
    private readonly IHubContext<GameHub, IGameHub> _hubContext = hubContext;
    private readonly PlayerConnectionMapper _connectionMapping = connectionMapping;

    /// <summary>
    /// Creates a new game and the game master who created it.
    /// </summary>
    /// <param name="model">The Dto for creating the game.</param>
    /// <response code="201">A new game was created.</response>
    /// <response code="400">Invalid values were provided.</response>
    [HttpPost]
    [ProducesResponseType(typeof(JoinedGameDto), StatusCodes.Status201Created, Application.Json)]
    public async Task<IActionResult> CreateSessionAsync([FromBody] JoinGameDto model)
    {
        if (!_manager.IsPlayerNameValid(model.PlayerName.Trim(), null))
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "A valid 'playerName' was expected!");
        
        (GameContext ctx, Player self, string auth) = await _manager.CreateGameAsync(model.PlayerName.Trim(), model.GamePassword);
        await _settingsStore.UpdateAsync(ctx.Id, (GameOptionsDto)_manager.GameOptions.DefaultOptions.Clone()).ConfigureAwait(false);     // Load the default game settings
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
    public async Task<IActionResult> GetAllSessionsAsync()
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
    public async Task<IActionResult> GetSessionById([FromRoute] int sessionId)
    {
        GameContext? ctx = await _manager.GetGameById(sessionId).ConfigureAwait(false);
        if (ctx?.State == GameState.NotInitialized)
            ctx = null;
        return ctx is not null
            ? Ok(new GameDto(ctx))
            : Problem(statusCode: StatusCodes.Status404NotFound, detail: "Session not found.");
    }

    /// <summary>
    /// Updates the settings of a game.
    /// </summary>
    /// <param name="sessionId">The game to update the settings.</param>
    /// <param name="options">The new options to set.</param>
    /// <response code="200">The dto.</response>
    /// <response code="400">Something is wrong with the send body,</response>
    /// <response code="403">You're not authorized to execute this endpoint.</response>
    /// <response code="404">The session wasn't found.</response>
    /// <returns>The updated settings.</returns>
    [Authorize]
    [HttpPut("{sessionId:int}/settings")]
    [ProducesResponseType(typeof(GameOptionsDto),StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> UpdateGameSettingsAsync([FromRoute] int sessionId, [FromBody] GameOptionsDto options)
    {
        if (await _manager.GetGameById(sessionId) is not { } ctx)
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Session not found.");
        if (HttpContext.User.GetPlayerId() != ctx.GameMaster.Id)
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You're not authorized to access this game.");
        foreach (KeyValuePair<int, int> role in options.AmountOfRoles.Where(role => role.Value <= 0))
            options.AmountOfRoles.Remove(role.Key);

        options.AmountOfRoles.TryGetValue((int)Role.Werwolf, out int wwAmount);
        if (wwAmount <= 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "At least one werwolf (0) is required!");
        if (RoleAttribute.GetRoles()
            .Where(attr => attr.FixedAmount != -1)
            .Any(attr =>
                options.AmountOfRoles.TryGetValue((int)attr.Role, out int amount) && amount != attr.FixedAmount ))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "A role which has a fixed amount is set to something else than this amount. See /api/config for reference.");
        }
        
        await _settingsStore.UpdateAsync(sessionId, options).ConfigureAwait(false);
        return Ok(options);
    }

    /// <summary>
    /// Retrieves the current settings of a game.
    /// </summary>
    /// <param name="sessionId">The id of the game to update.</param>
    /// <response code="200">The dto.</response>
    /// <response code="404">The session wasn't found.</response>
    /// <returns>The dto.</returns>
    [Authorize]
    [HttpGet("{sessionId:int}/settings")]
    [ProducesResponseType(typeof(GameOptionsDto),StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> GetGameSettingsAsync([FromRoute] int sessionId)
    {
        if (HttpContext.User.GetGameId() != sessionId)
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You're not authorized to access this game.");
        GameOptionsDto? options = await _settingsStore.GetAsync(sessionId).ConfigureAwait(false);
        return options is not null ? Ok(options) : Problem(statusCode: StatusCodes.Status404NotFound, detail: "Session not found.");
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
    [HttpPost("{sessionId:int}/players")]
    [ProducesResponseType(typeof(JoinedGameDto), StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> AddPlayerAsync([FromRoute] int sessionId, [FromBody] JoinGameDto model)
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

    /// <summary>
    /// Retrieves all players from a game.
    /// </summary>
    /// <param name="sessionId">The game to get the player from.</param>
    /// <returns>The retrieved players.</returns>
    /// <response code="200">The players of the game.</response>
    /// <response code="404">The specified game wasn't found.</response>
    [Authorize]
    [HttpGet("{sessionId:int}/players")]
    [ProducesResponseType(typeof(PlayerDto[]), StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> GetAllPlayersAsync([FromRoute] int sessionId)
    {
        if (HttpContext.User.GetGameId() != sessionId)
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You're not authorized to access this game.");
        
        GameContext? ctx = await _manager.GetGameById(sessionId);
        return ctx is not null
            ? Ok(ctx.Players.ToDtoCollection())
            : Problem(statusCode: StatusCodes.Status404NotFound, detail: "Game wasn't not found.");
    }
    
    /// <summary>
    /// Retrieves a single player from a game.
    /// </summary>
    /// <param name="sessionId">The game to get the player from.</param>
    /// <param name="playerId">The player to retrieve.</param>
    /// <returns>The retrieved player.</returns>
    /// <response code="200">The retrieved player.</response>
    /// <response code="404">The specified game or player wasn't found.</response>
    [Authorize]
    [HttpGet("{sessionId:int}/players/{playerId:int}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK, Application.Json)]
    public async Task<IActionResult> GetPlayerByIdAsync([FromRoute] int sessionId, [FromRoute] int playerId)
    {
        if (HttpContext.User.GetGameId() != sessionId)
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You're not authorized to access this game.");
        
        GameContext? ctx = await _manager.GetGameById(sessionId);
        Player? player = ctx?.Players.SingleOrDefault(p => p.Id == playerId);
        return ctx is null || player is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: "Game or player not found.")
            : Ok(new PlayerDto(player));
    }
    
    /// <summary>
    /// Removes a player from an existing game session. You can always remove yourself, but to remove other players you need to be the game master.
    /// </summary>
    /// <param name="sessionId">The game to remove the player from.</param>
    /// <param name="playerId">The player to remove.</param>
    [Authorize]
    [HttpDelete("{sessionId:int}/players/{playerId:int}")]
    public async Task<IActionResult> RemovePlayerAsync([FromRoute] int sessionId, [FromRoute] int playerId)
    {
        int selfId = HttpContext.User.GetPlayerId();
        GameContext? ctx = await _manager.GetGameById(sessionId);
        Player? playerToKick = ctx?.Players.SingleOrDefault(p => p.Id == playerId);
        if (ctx is null || playerToKick is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Game or player not found.");
        
        if (playerId != selfId && ctx.GameMaster.Id != selfId)
        {
            _logger.LogWarning("Non-game-master {playerId} tried to kick player {playerToKick} (rejected)", selfId, playerId);
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Only the game master is allowed to kick other players!");
        }
        
        string[] playerConnections = _connectionMapping.GetPlayerConnections(ctx.Id, playerToKick.Id);
        foreach (string connectionId in playerConnections)
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, GroupNames.Game(ctx.Id));
        
        await _manager.LeaveGameAsync(ctx, playerToKick);
        await _hubContext.Clients.Player(ctx.Id, playerToKick.Id).ForceDisconnect(kicked: playerId != selfId);     // Not done by manager because it can't differentiate between leaving and kicking
        
        if (ctx.Players.Count == 0)
            await _settingsStore.RemoveAsync(ctx.Id).ConfigureAwait(false);
        return Ok();
    }
}