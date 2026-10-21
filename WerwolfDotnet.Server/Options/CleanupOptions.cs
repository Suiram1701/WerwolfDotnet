namespace WerwolfDotnet.Server.Options;

public class CleanupOptions
{
    /// <summary>
    /// The interval in which the cleanup service in run
    /// </summary>
    public TimeSpan Intervall { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// The time after which an inactive session is terminated. A session is called inactive when the last log was done after the specified time span.
    /// </summary>
    public TimeSpan? AfterInactivity { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// After which an empty session is terminated. A session is called empty when only one player (the game master is in it).
    /// </summary>
    public TimeSpan? AfterEmptySession { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// When a player didn't participate in n action in a row he gets kicked.
    /// </summary>
    public int KickPlayerAfterInactiveActions { get; set; } = 5;
}