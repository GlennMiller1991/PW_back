using Microsoft.EntityFrameworkCore;
using webapi.Infrastructure.Data;
using webapi.Infrastructure.Repositories;
using webapi.Services;
using webapi.Services.AuthService;
using webapi.Services.GameService;

namespace webapi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration
        )
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        
        services.AddScoped<UserRepository, UserRepository>();
        services.AddScoped<SessionRepository, SessionRepository>(); 
        services.AddScoped<DatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<PixelRepository, PixelRepository>();

        return services;
    }

    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddSingleton<JwtService, JwtService>();
        services.AddSingleton<GameService, GameService>();
        services.AddScoped<AuthService, AuthService>();

        return services;
    }
}