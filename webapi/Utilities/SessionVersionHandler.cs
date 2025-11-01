using Microsoft.AspNetCore.Authorization;
using webapi.Extensions;
using webapi.Infrastructure.Repositories;

namespace webapi.Utilities;

public class SessionVersionRequirements : IAuthorizationRequirement;

public class SessionVersionHandler<T>(SessionRepository sessionRepository)
    : AuthorizationHandler<T> where T : SessionVersionRequirements
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        T requirement)
    {
        if (await SessionVersionHandle(context)) context.Succeed(requirement);
        else context.Fail();
    }

    protected async Task<bool> SessionVersionHandle(AuthorizationHandlerContext context)
    {
        var claimSession = context.User.GetSessionVersion();
        var claimUser = context.User.GetUserId();
        var session = await sessionRepository.GetByUserIdAsync(claimUser);
        if (session?.Version != claimSession)
            return false;

        return true;
    }
}