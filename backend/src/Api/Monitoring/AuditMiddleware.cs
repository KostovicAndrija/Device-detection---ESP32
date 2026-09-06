using Domain.Entities;
using Infrastructure.Persistence;

namespace Api.Monitoring;

public sealed class AuditMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AuditedMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        await next(context);

        if (!AuditedMethods.Contains(context.Request.Method) ||
            context.User.Identity?.IsAuthenticated != true ||
            context.Request.Path.StartsWithSegments("/api/auth"))
        {
            return;
        }

        var entry = AuditLog.Create(
            context.User.Identity.Name ?? "unknown",
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Response.StatusCode);
        await dbContext.AuditLogs.AddAsync(entry, context.RequestAborted);
        await dbContext.SaveChangesAsync(context.RequestAborted);
    }
}
