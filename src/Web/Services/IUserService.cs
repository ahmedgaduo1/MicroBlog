using MicroBlog.Web.Models;
using Microsoft.AspNetCore.Http;

namespace MicroBlog.Web.Services;

/**
 * Interface for user-related operations.
 */
public interface IUserService
{
    Task<UserProfileViewModel> GetUserProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, string? bio, IFormFile? profileImage);
    Task<IEnumerable<UserViewModel>> GetSuggestedUsersAsync(string userId);
    Task FollowUserAsync(string currentUserId, string targetUserId);
    Task UnFollowUserAsync(string currentUserId, string targetUserId);
}
