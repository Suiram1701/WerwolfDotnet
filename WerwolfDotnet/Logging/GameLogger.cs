using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WerwolfDotnet.Logging;

public sealed class GameLogger(ILogger? internalLogger = null)
{
    public IReadOnlyList<LogMessage> Messages => _messages.AsReadOnly();
    private readonly List<LogMessage> _messages = [];

    public event Action<LogMessage>? OnMessage;

    /// <summary>
    /// A logger intended for internal logs (never shipped to the user)
    /// </summary>
    internal ILogger InternalLogger { get; } = internalLogger ?? NullLogger.Instance;
    
    internal void Log(Event @event, Player? source, Player? target) => Log(@event, source, target is null ? [] : [target]);

    internal void Log(Event @event, Player? source, Player[] targets) => Log(@event, args: [source, ..targets]);
    
    internal void Log(Event @event, params object?[] args)
    {
        LogMessage msg = new(DateTimeOffset.UtcNow, @event, [.. args.Select(TrySnapshotArg)]);
        _messages.Add(msg);
        OnMessage?.Invoke(msg);
    }

    private static object? TrySnapshotArg(object? arg) => arg switch
    {
        Player player => new LogPlayerSnapshot(player),
        Player[] players => players.Select(p => new LogPlayerSnapshot(p)).ToArray(),
        KeyValuePair<Player, Player[]> kvp => KeyValuePair.Create(new LogPlayerSnapshot(kvp.Key), (LogPlayerSnapshot[])TrySnapshotArg(kvp.Value)!),
        _ => arg
    };
}