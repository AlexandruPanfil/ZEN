namespace ZEN.Domain.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
