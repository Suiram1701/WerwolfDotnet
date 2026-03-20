using System.Reflection;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RoleAttribute(Role role) : Attribute
{
    public Role Role => role;

    /// <summary>
    /// The amount of players which should have this role when it exists (-1 when its irrelevant).
    /// </summary>
    /// <remarks>
    /// Note: This isn't validated by <see cref="GameContext"/> because it isn't necessary for the game work.
    /// </remarks>
    public int FixedAmount { get; init; } = -1;
    
    private static readonly Dictionary<Type, RoleAttribute> _roleTypeCache = new();
    private static readonly Lock _lock = new();

    public static IEnumerable<RoleAttribute> GetRoles()
    {
        EnsureCache();
        return _roleTypeCache.Values;
    }
    
    public static Role? GetRole(Type type)
    {
        EnsureCache();
        _roleTypeCache.TryGetValue(type, out RoleAttribute? attr);
        return attr?.Role;
    }

    public static Type? GetRoleType(Role role)
    {
        EnsureCache();
        return _roleTypeCache.FirstOrDefault(kvp => kvp.Value.Role == role).Key;
    }

    private static void EnsureCache()
    {
        lock (_lock)
        {
            if (_roleTypeCache.Count != 0)
                return;
            foreach (Type type in Assembly.GetAssembly(typeof(RoleBase))!.GetTypes()
                         .Where(t => t.IsAssignableTo(typeof(RoleBase)) && !t.IsAbstract))
            {
                if (type.GetCustomAttribute<RoleAttribute>() is not { } attr)
                    continue;
                if (_roleTypeCache.Values.Any(a => a.Role == attr.Role))
                    throw new InvalidOperationException($"Duplicate role attribute: {attr.Role} on {type}");
                _roleTypeCache.Add(type, attr);
            }
        }
    }
}