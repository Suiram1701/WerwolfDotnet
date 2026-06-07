namespace WerwolfDotnet.Server.Options;

public class CleanupOptions
{
    public TimeSpan Intervall { get; set; } = TimeSpan.FromMinutes(60);
    
    public TimeSpan? AfterInactivity { get; set; } = TimeSpan.FromHours(24);

    public TimeSpan? AfterEmptySession { get; set; } = null;
}