using Microsoft.AspNetCore.Http;

namespace ContactCenterAI.Infrastructure.Identity;

public class LocalUserResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public LocalUserResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILocalUserResolver localUserResolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var resolution = await localUserResolver.ResolveAsync(context.User, context.RequestAborted);
            context.Items[LocalUserContextKeys.Resolution] = new LocalUserContext
            {
                Resolution = resolution
            };
        }

        await _next(context);
    }
}
