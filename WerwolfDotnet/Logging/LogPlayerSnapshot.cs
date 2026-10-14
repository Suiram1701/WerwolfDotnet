using System.Diagnostics;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet.Logging;

[DebuggerDisplay("{ToString(),nq}")]
public class LogPlayerSnapshot(int id, string name, Role? role)
{
    public LogPlayerSnapshot(Player player) : this(player.Id, player.Name, player.Role?.Type)
    {
    }

    public int Id { get; } = id;

    public string Name { get; } = name;

    public Role? Role { get; } = role;

    public override string ToString() => $"{Name} ({Id})";

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        return obj switch
        {
            Player p => p.Id == Id && p.Name == Name,
            LogPlayerSnapshot pSnapshot => pSnapshot.Id == Id && pSnapshot.Name == Name && pSnapshot.Role == Role,
            _ => false
        };
    }

    public override int GetHashCode() => HashCode.Combine(Id, Name, Role);
}