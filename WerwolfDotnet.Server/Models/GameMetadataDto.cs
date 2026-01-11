namespace WerwolfDotnet.Server.Models;

public class GameMetadataDto(GameContext game)
{
    public int GameMasterId { get; } = game.GameMaster.Id;

    public int? MayorId { get; } = game.Mayor?.Id;
}