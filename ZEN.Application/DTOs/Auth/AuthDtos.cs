namespace ZEN.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string UserId, string UserName, string Email, IEnumerable<string> Roles);

public record PinLoginRequest(string PIN);

public record RegisterUserRequest(string Email, string FullName, string Password, string? PIN, IEnumerable<string> Roles);

public record UserDto(string Id, string Email, string FullName, string? PIN, bool IsActive, IEnumerable<string> Roles);
