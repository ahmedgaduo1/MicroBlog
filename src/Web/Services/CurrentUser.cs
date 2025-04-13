using System.Security.Claims;

using MicroBlog.Application.Common.Interfaces;

namespace MicroBlog.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessors)
    {
        _httpContextAccessor = httpContextAccessors;
    }

    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
