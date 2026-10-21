using Microsoft.AspNetCore.Authentication;

namespace WerwolfDotnet.Server.Authentication;

public class AdminAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Indicates whether admin authentication is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// A mapping of admin users to their passwords.
    /// </summary>
    public Dictionary<string, string> Users { get; set; } = [];
}