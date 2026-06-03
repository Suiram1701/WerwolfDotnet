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
}