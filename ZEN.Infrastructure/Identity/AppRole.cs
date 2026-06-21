using Microsoft.AspNetCore.Identity;

namespace ZEN.Infrastructure.Identity;

public class AppRole : IdentityRole
{
    public string? Description { get; set; }

    public AppRole() { }
    public AppRole(string roleName) : base(roleName) { }
}
