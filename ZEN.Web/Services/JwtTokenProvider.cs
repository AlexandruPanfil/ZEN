using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZEN.Domain.Interfaces;

namespace ZEN.Web.Services;

public class JwtTokenProvider
{
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public JwtTokenProvider(IConfiguration configuration, ICurrentUserService currentUserService)
    {
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public string GetToken()
    {
        var keyString = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString))
            throw new InvalidOperationException("JWT Key is missing from configuration!");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _currentUserService.UserId ?? string.Empty),
            new(ClaimTypes.Name, _currentUserService.UserName ?? string.Empty),
            new(ClaimTypes.Email, _currentUserService.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(_currentUserService.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (_currentUserService.CashDeskId.HasValue)
        {
            claims.Add(new Claim("cash_desk_id", _currentUserService.CashDeskId.Value.ToString()));
        }

        if (_currentUserService.StoreId.HasValue)
        {
            claims.Add(new Claim("store_id", _currentUserService.StoreId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}