using Google.Apis.Auth;
using webapi.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace webapi.Services;

public class AuthService(
    UserRepository userRepository,
    SessionRepository sessionRepository,
    JwtService jwtService
    )
{

    public async Task<(string, string, DateTime)> Refresh(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        var refreshTokenHash = Convert.ToBase64String(hashBytes);
        var session = await sessionRepository.GetByTokenHash(refreshTokenHash);
        if (session == null) throw new Exception("Session not found");
        sessionRepository.Delete(session);
        return await Auth(session.UserId, ++session.Version);
    }
    public async Task<(string, string, DateTime)> AuthWithGoogle(string idToken)
    { 
        GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
        
        var user = await userRepository.GetOrCreateByGoogleIdAsync(payload.Subject);
        var session = await sessionRepository.GetByUserIdAsync(user.Id);
        if (session != null) sessionRepository.Delete(user.Id);

        var sessionVersion = session?.Version ?? 0;
        sessionVersion++;
            
        return await Auth(user.Id, sessionVersion);
    }

    private async Task<(string, string, DateTime)> Auth(int userId, int sessionVersion)
    {
        var accessToken= jwtService.GenerateAccessToken(userId, sessionVersion);
        var (refreshToken , refreshTokenHash) = jwtService.GenerateRefreshToken();

        var refreshExpiresAt = DateTime.UtcNow.AddMinutes(60 * 24 * 7);

        await sessionRepository.CreateAsync(userId, sessionVersion, refreshTokenHash, refreshExpiresAt);

        return (accessToken, refreshToken, refreshExpiresAt);
    }
}