namespace ZEN.Domain.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    Guid? StoreId { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
}
