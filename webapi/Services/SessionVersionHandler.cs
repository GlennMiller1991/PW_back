using Microsoft.AspNetCore.Authorization;
using webapi.Extensions;
using webapi.Infrastructure.Repositories;

namespace webapi.Services;

public class SessionVersionRequirements : IAuthorizationRequirement
{
}

public class SessionVersionHandler(SessionRepository sessionRepository)
    : AuthorizationHandler<SessionVersionRequirements>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        SessionVersionRequirements requirement)
    {
        var claimSession = context.User.GetSessionVersion();
        var claimUser = context.User.GetUserId();
        var session = await sessionRepository.GetByUserIdAsync(claimUser);
        if (session?.Version != claimSession)
        {
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}