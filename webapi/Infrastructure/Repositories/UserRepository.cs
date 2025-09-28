using Microsoft.EntityFrameworkCore;
using webapi.Infrastructure.Data;
using webapi.Infrastructure.Data.Entities;

namespace webapi.Infrastructure.Repositories;

public class UserRepository(AppDbContext context)
{
    private readonly AppDbContext _context = context;

    private async Task<User> Create(string googleId)
    {
        var user = new User { GoogleId = googleId };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> GetOrCreateByGoogleIdAsync(string googleId)
    {
        var user = await GetByGoogleIdAsync(googleId);
        if (user == null) user = await Create(googleId);

        return user;
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }
}