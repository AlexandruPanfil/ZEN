using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Auth;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async ([FromBody] LoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var response = await authService.LoginAsync(request, ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("Login")
        .WithOpenApi();

        group.MapPost("/pin-login", async ([FromBody] PinLoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var response = await authService.PinLoginAsync(request, ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("PinLogin")
        .WithOpenApi();

        group.MapPost("/register", async ([FromBody] RegisterUserRequest request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var user = await authService.RegisterUserAsync(request, ct);
                return Results.Created($"/api/auth/users/{user.Id}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("RegisterUser")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/users", async (IAuthService authService, CancellationToken ct) =>
        {
            var users = await authService.GetAllUsersAsync(ct);
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/users/{userId}", async (string userId, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var user = await authService.GetUserByIdAsync(userId, ct);
                return Results.Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("GetUserById")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
