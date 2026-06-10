using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RB_Website_API.Auth;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string role, string? email);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string CreateToken(Guid userId, string role, string? email)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key))
            throw new InvalidOperationException("Jwt:Key is not configured.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, (role ?? "").Trim().ToLowerInvariant()),
        };

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new(JwtRegisteredClaimNames.Email, email));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddMinutes(Math.Max(5, _settings.ExpMinutes));

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(_settings.Issuer) ? null : _settings.Issuer,
            audience: string.IsNullOrWhiteSpace(_settings.Audience) ? null : _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

