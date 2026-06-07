namespace WerwolfDotnet.Server.Models;

public class ClientConfigDto
{
    public bool SessionsVisible { get; init; }
    
    public int PlayerNameMinLength { get; init; }
    
    public int PlayerNameMaxLength { get; init; }
    
    public bool PlayerNameAllowNumbers { get; init; }
    
    public int MinimumPlayers { get; init; }

    public Dictionary<int, int> FixedRoleAmounts { get; init; } = [];
    
    public bool CanStartWhenNotReady { get; init; }
    
    public bool GameMasterSkipAllowed { get; init; }
}