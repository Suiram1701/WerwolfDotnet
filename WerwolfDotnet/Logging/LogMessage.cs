namespace WerwolfDotnet.Logging;

public record LogMessage(DateTimeOffset Time, Event @Event, object?[] Args);
