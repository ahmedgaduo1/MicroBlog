using System.Security.Claims;
using MicroBlog.Application.Common.Models;

namespace MicroBlog.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    Task<string> GetUserIdAsync(ClaimsPrincipal user);
    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    // JWT Authentication Methods
    Task<(Result Result, string Token)> AuthenticateAsync(string userName, string password);

    Task<string> GenerateJwtTokenAsync(string userId, string userName);

    Task<bool> ValidateTokenAsync(string token);

}
