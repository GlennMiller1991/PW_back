using Microsoft.EntityFrameworkCore;
using webapi.Infrastructure.Data;
using webapi.Infrastructure.Data.Entities;

namespace webapi.Infrastructure.Repositories;

public class SessionRepository(AppDbContext context)
{

    public async Task<Session?> GetByTokenHash(string hash)
    {
        return await context.Sessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash);
    }
    public async Task<Session?> GetByUserIdAsync(int userId)
    {
        return await context.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Session> CreateAsync(int userId, int sessionVersion, string refreshTokenHash, DateTime expiresAt)
    {
        var s = new Session
        {
            UserId = userId,
            Version = sessionVersion,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = expiresAt,
        };
        await context.Sessions.AddAsync(s);
        await context.SaveChangesAsync();

        return s;
    }

    public void Delete(int userId)
    {
        var s = context.Sessions.FirstOrDefault(s => s.UserId == userId);
        if (s == null) return;
        Delete(s);
    }

    public void Delete(Session session)
    {
        context.Sessions.Remove(session);
        context.SaveChanges();
    }
    
    
    
}