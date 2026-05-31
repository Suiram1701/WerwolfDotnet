namespace WerwolfDotnet.Logging;

public sealed class GameLogger
{
    public IReadOnlyList<LogMessage> Messages => _messages.AsReadOnly();
    private readonly List<LogMessage> _messages = [];

    public event Action<LogMessage>? OnMessage;

    internal void Log(Event @event, Player? source, Player? target) => Log(@event, source, target is null ? [] : [target]);

    internal void Log(Event @event, Player? source, Player[] targets) => Log(@event, args: [source, ..targets]);
    
    internal void Log(Event @event, params object?[] args)
    {
        LogMessage msg = new(DateTimeOffset.UtcNow, @event, args
            .Select(arg => arg is Player p ? new LogPlayerSnapshot(p) : arg)
            .ToArray());
        _messages.Add(msg);
        OnMessage?.Invoke(msg);
    }
}