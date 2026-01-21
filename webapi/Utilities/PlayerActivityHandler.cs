using Microsoft.AspNetCore.Authorization;
using webapi.Extensions;
using webapi.Infrastructure.Repositories;
using webapi.Services.GameInfra;

namespace webapi.Utilities;

public class PlayerActivityRequirements : SessionVersionRequirements;

public class PlayerActivityHandler<T>(SessionRepository sessionRepository, GameService gameService) :
    SessionVersionHandler<PlayerActivityRequirements>(sessionRepository) where T : PlayerActivityRequirements
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PlayerActivityRequirements requirements)
    {
        if (!await SessionVersionHandle(context))
        {
            context.Fail();
            return;
        }

        var userId = context.User.GetUserId();
        var player = gameService.ActivePlayers.GetByUserId(userId);
        if (player?.Role != GameRole.Player)
        {
            context.Fail();
            return;
        }

        var httpContext = context.Resource as HttpContext;

        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        httpContext.Items["Player"] = player;
        context.Succeed(requirements);
    }
}