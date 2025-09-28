using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace webapi.Services;

public class JwtService
{
    public readonly SymmetricSecurityKey Key;
    public readonly string Audience;
    private readonly SigningCredentials _credentials;
    private readonly int _accessTokenLifetimeInMinutes;

    public JwtService(IConfiguration configuration)
    {
        Audience = configuration["Audience"]!;
        Key = new SymmetricSecurityKey(GetRandomByteArray());
        
        _accessTokenLifetimeInMinutes = int.TryParse(configuration["AccessTokenLifetimeInMinutes"]!, out var lifetime) ? lifetime : 15;
        _credentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateAccessToken(int userId, int sessionVersion)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("session_version", sessionVersion.ToString()),
        };

        var token = new JwtSecurityToken(
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenLifetimeInMinutes),
            signingCredentials: _credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public (string refreshToken, string refreshTokenHash) GenerateRefreshToken()
    {

        var random = GetRandomByteArray();
        var refreshToken = Convert.ToBase64String(random);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        var refreshTokenHash = Convert.ToBase64String(hashBytes);

        return (refreshToken, refreshTokenHash);
    }
    
    public byte[] GetRandomByteArray()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return randomNumber;
    }
}