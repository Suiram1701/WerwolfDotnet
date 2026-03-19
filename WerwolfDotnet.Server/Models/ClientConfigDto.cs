namespace WerwolfDotnet.Server.Models;

public class ClientConfigDto
{
    public bool SessionsVisible { get; init; }
    
    public int MinimumPlayers { get; init; }
    
    public int PlayerNameMinLength { get; init; }
    
    public bool GameMasterSkipAllowed { get; init; }
}