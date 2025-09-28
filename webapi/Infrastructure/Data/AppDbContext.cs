using Microsoft.EntityFrameworkCore;
using webapi.Infrastructure.Data.Entities;

namespace webapi.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }

    public DbSet<Session> Sessions { get; set; }
}