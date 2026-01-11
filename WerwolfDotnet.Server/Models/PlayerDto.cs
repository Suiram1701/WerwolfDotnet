namespace WerwolfDotnet.Server.Models;

public class PlayerDto(Player player)
{
    /// <summary>
    /// The id (only unique in the game session) of the player.
    /// </summary>
    public int Id { get; } = player.Id;
    
    /// <summary>
    /// The name of the player.
    /// </summary>
    public string Name { get; } = player.Name;

    /// <summary>
    /// Indicates whether this player is currently alive.
    /// </summary>
    public bool Alive { get; } = player.Status != PlayerState.Death;     // Also pending death should be displayed as alive (yet)
}