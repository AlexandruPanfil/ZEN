namespace ZEN.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string UserId, string UserName, string Email, IEnumerable<string> Roles, Guid? CashDeskId = null, Guid? StoreId = null);

public record PinLoginRequest(string PIN);

public record RegisterUserRequest(string Email, string FullName, string Password, string? PIN, IEnumerable<string> Roles, Guid? CashDeskId = null);

public record UserDto(string Id, string Email, string FullName, string? PIN, bool IsActive, IEnumerable<string> Roles, Guid? CashDeskId = null, string? CashDeskName = null)
{
    public UserDto() : this(
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        false,
        Enumerable.Empty<string>(),
        null,
        null)
    {
    }
}

public record RoleDto(string Id, string Name, string? Description)
{
    public RoleDto() : this(string.Empty, string.Empty, null) { }
}