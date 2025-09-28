using webapi.Infrastructure.Data;

namespace webapi.Services;

public class DatabaseInitializer(AppDbContext context)
{

    public async Task InitializeAsync()
    {
        context.Sessions.RemoveRange(context.Sessions);
        await context.SaveChangesAsync();
    }
}