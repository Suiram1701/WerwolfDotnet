using WerwolfDotnet.Logging;

namespace WerwolfDotnet.Server.Models;

public class LogMessageDto(LogMessage msg, bool withRoles)
{
    public DateTimeOffset TimeStamp { get; } = msg.Time.ToLocalTime();

    public Event EventType { get; } = msg.Event;
    
    public object?[] Args { get; } = msg.Args.Select(arg => arg switch
    {
        // I know the logging shit is quite messy but there isn't really a way to implement this in a good and at the same time abstract way.
        LogPlayerSnapshot snapshot => withRoles ? snapshot : new LogPlayerSnapshot(snapshot.Id, snapshot.Name, null),           // Make it possible to filter out roles
        KeyValuePair<Player, Player[]> kvp => KeyValuePair.Create(kvp.Key.Name, kvp.Value.Select(p => p.Name).ToArray()),     // Passed from Event.Voting calls.
        Enum e => e,     // Returns in reality an int
        null => (object?)null,
        _ => throw new ArgumentException($"Argument type {arg.GetType()} isn't supported!", nameof(arg))     // Make sure not return not reviewed objects
    }).ToArray();
}