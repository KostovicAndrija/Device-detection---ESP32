using Application.Abstractions.Security;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Api.Auth;

public sealed class SessionAuthorizationFilter(ISessionAccess access, ICurrentUser user, AppDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (user.IsAssistant)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            string? sessionId = context.ActionArguments.TryGetValue("sessionId", out var sessionValue) ? sessionValue?.ToString() : null;
            if (controller == "Sessions")
            {
                sessionId = context.ActionArguments.TryGetValue("id", out var idValue) ? idValue?.ToString() : null;
                if (context.HttpContext.Request.Path.Value!.Contains("/whitelist") && context.HttpContext.Request.Method != "GET")
                    throw new UnauthorizedAccessException("Listom dozvoljenih uređaja upravlja profesor.");
            }
            if (controller == "Alerts" && context.ActionArguments.TryGetValue("id", out var alertId))
            {
                sessionId = await db.Alerts.Where(a => a.Id == (Guid)alertId!).Select(a => a.SessionId).SingleOrDefaultAsync();
                if (sessionId is null) throw new UnauthorizedAccessException();
            }
            if (sessionId is not null)
            {
                if (!Guid.TryParse(sessionId, out var id)) throw new UnauthorizedAccessException();
                await access.EnsureAccessAsync(id, context.HttpContext.RequestAborted);
            }
        }
        await next();
    }
}
