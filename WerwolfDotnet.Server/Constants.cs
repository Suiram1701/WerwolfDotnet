namespace WerwolfDotnet.Server;

public static class Constants
{
    public static class Claims
    {
        public const string SessionId = "sessionId";
        public const string PlayerId = "playerId";
        public const string PlayerName = "playerName";
    }
    
    public const string GameHubPath = "/signalr/game";

    public const string GameSessionIdParam = "sessionId";
    
    public const string PlayerIdParam = "playerId";

    public static class GroupNames
    {
        public static string Game(int gameId) => $"game:{gameId}";
    } 
}