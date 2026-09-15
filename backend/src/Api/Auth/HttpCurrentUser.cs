using System.Security.Claims;
using Application.Abstractions.Security;

namespace Api.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsProfessor => accessor.HttpContext?.User.IsInRole("Professor") == true;
    public bool IsAssistant => accessor.HttpContext?.User.IsInRole("Assistant") == true;
}
