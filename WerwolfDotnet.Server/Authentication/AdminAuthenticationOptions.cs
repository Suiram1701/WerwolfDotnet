using Microsoft.AspNetCore.Authentication;

namespace WerwolfDotnet.Server.Authentication;

public class AdminAuthenticationOptions : AuthenticationSchemeOptions
{
    public bool Enabled { get; set; } = false;
    
    public Dictionary<string, string> Users { get; set; } = [];
}