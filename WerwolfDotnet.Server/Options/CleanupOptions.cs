namespace WerwolfDotnet.Server.Options;

public class CleanupOptions
{
    public TimeSpan Intervall { get; set; } = TimeSpan.FromMinutes(5);
    
    public TimeSpan? AfterInactivity { get; set; } = TimeSpan.FromHours(12);

    public TimeSpan? AfterEmptySession { get; set; } = TimeSpan.FromMinutes(30);
    
    public int KickPlayerAfterInactiveActions { get; set; } = 5;
}