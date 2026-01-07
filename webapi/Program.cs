using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using webapi.Extensions;
using webapi.Services;
using webapi.Utilities;

var builder = WebApplication.CreateBuilder(args);
WebApplication? app = null;
builder.Services
    .AddDatabaseServices(builder.Configuration)
    .AddBusinessLogic();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            AudienceValidator = (audience, _, _) =>
            {
                using var scope = app.Services.CreateScope();
                var jwtConfig = scope.ServiceProvider.GetRequiredService<JwtService>();
                return audience.Contains(jwtConfig.Audience);
            },
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, parameters) =>
            {
                using var scope = app.Services.CreateScope();
                var jwtConfig = scope.ServiceProvider.GetRequiredService<JwtService>();
                return [jwtConfig.Key];
            },
            
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                Console.WriteLine(context.Principal.Identity);
                await Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ValidSession", policy =>
        policy.Requirements.Add(new SessionVersionRequirements()));
    
    options.AddPolicy("ActivePlayer", policy => 
        policy.Requirements.Add(new PlayerActivityRequirements()));
    
    options.AddPolicy("ValidPlayer", policy => 
        policy.Requirements.Add(new PlayerValidityRequirements()));
});
builder.Services.AddScoped<IAuthorizationHandler, SessionVersionHandler<SessionVersionRequirements>>();
builder.Services.AddScoped<IAuthorizationHandler, PlayerActivityHandler<PlayerActivityRequirements>>();

app = builder.Build();

app.UseWebSockets();
app.UseMiddleware<DebugMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetService<DatabaseInitializer>();
    initializer?.InitializeAsync().GetAwaiter().GetResult();
}
app.MapControllers();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Run();
