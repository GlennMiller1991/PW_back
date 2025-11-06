using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using webapi.Infrastructure.Data.Entities;
using webapi.Infrastructure.Repositories;

namespace webapi.Services.AuthService;

public class AuthService(
    UserRepository userRepository,
    SessionRepository sessionRepository,
    JwtService jwtService,
    GameService.GameService gameService
    )
{

    public async Task<(string, string, DateTime)> Refresh(string refreshToken)
    {
        var session = await ValidateRefreshToken(refreshToken);
        await sessionRepository.DeleteAsync(session);
        return await Auth(session.UserId, ++session.Version);
    }

    public async Task<Session> ValidateRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        var refreshTokenHash = Convert.ToBase64String(hashBytes);
        var session = await sessionRepository.GetByTokenHash(refreshTokenHash);
        return session ?? throw new Exception("Session not found");
    }
    
    public async Task<(string, string, DateTime)> AuthWithGoogle(string idToken)
    { 
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
        if (!payload.EmailVerified) throw new GoogleAuthException();
        
        var user = await userRepository.GetOrCreateByGoogleIdAsync(payload.Subject);
        var session = await sessionRepository.GetByUserIdAsync(user.Id);
        if (session != null) await sessionRepository.DeleteAsync(user.Id);

        _ = gameService.ActivePlayers.RemovePlayer(user.Id);

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