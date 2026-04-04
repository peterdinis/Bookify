using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bookify.API.Models;
using Bookify.API.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bookify.API.Services;

public class SessionJwtService(IOptions<SessionJwtOptions> options)
{
    private readonly SessionJwtOptions _opt = options.Value;

    public string CreateAccessToken(User user, string? displayName, string? email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("entra_oid", user.EntraId),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        if (!string.IsNullOrWhiteSpace(displayName))
            claims.Add(new Claim(ClaimTypes.Name, displayName));
        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
