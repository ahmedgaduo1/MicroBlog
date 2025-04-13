// Ignore Spelling: bio

using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Entities;
using MicroBlog.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MicroBlog.Web.Services;

/**
 * Service for handling user-related operations.
 */
public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IApplicationDbContext context,
        IImageProcessingService imageProcessingService,
        ILogger<UserService> logger)
    {
        _context = context;
        _imageProcessingService = imageProcessingService;
        _logger = logger;
    }

    /**
     * Gets a user's profile information.
     */
    public async Task<UserProfileViewModel> GetUserProfileAsync(string userId)
    {
        _logger.LogInformation("Getting profile for user {UserId}", userId);
        try
        {
            var user = await _context.Users
                .Include(u => u.Following)
                .Include(u => u.Followers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                throw new ArgumentException($"User with ID {userId} not found");
            }

            var posts = await _context.Posts
                .Include(p => p.Images)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved profile for user {UserId} with {PostCount} posts, {FollowerCount} followers, and {FollowingCount} following",
                userId, posts.Count, user.Followers.Count, user.Following.Count);
                
            return new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl,
                Bio = user.Bio,
                PostCount = posts.Count,
                FollowerCount = user.Followers.Count,
                FollowingCount = user.Following.Count,
                Posts = posts.Select(p => new PostViewModel
                {
                    Id = p.Id,
                    Text = p.Text,
                    ImageUrl = p.ImageUrl,
                    UserName = user.UserName ?? string.Empty,
                    UserProfileImageUrl = user.ProfileImageUrl,
                    PostedAt = p.CreatedAt,
                    LikeCount = p.LikeCount
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
            throw;
        }
    }

    /**
     * Updates a user's profile information.
     */
    public async Task UpdateProfileAsync(string userId, string? bio, IFormFile? profileImage)
    {
        _logger.LogInformation("Updating profile for user {UserId}", userId);
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found during profile update", userId);
                throw new ArgumentException($"User with ID {userId} not found");
            }

            if (bio != null)
            {
                _logger.LogDebug("Updating bio for user {UserId}", userId);
                user.Bio = bio;
            }

            if (profileImage != null)
            {
                _logger.LogDebug("Updating profile image for user {UserId}", userId);
                user.ProfileImageUrl = await _imageProcessingService.SaveOriginalImageAsync(profileImage);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated profile for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            throw;
        }
    }

    /**
     * Gets a list of suggested users to follow.
     */
    public async Task<IEnumerable<UserViewModel>> GetSuggestedUsersAsync(string userId)
    {
        _logger.LogInformation("Getting suggested users for user {UserId}", userId);
        try
        {
            var user = await _context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found while getting suggested users", userId);
                throw new ArgumentException($"User with ID {userId} not found");
            }

            var followingIds = user.Following.Select(f => f.Id).ToList();
            followingIds.Add(userId); // Don't suggest the current user

            var suggestedUsers = await _context.Users
                .Where(u => !followingIds.Contains(u.Id))
                .Take(10)
                .ToListAsync();

            _logger.LogDebug("Found {Count} suggested users for user {UserId}", suggestedUsers.Count, userId);
            
            return suggestedUsers.Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                ProfileImageUrl = u.ProfileImageUrl,
                Bio = u.Bio,
                IsFollowed = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggested users for user {UserId}", userId);
            throw;
        }
    }

    /**
     * Follows a user.
     */
    public async Task FollowUserAsync(string currentUserId, string targetUserId)
    {
        _logger.LogInformation("User {CurrentUserId} is attempting to follow user {TargetUserId}", currentUserId, targetUserId);
        try
        {
            if (currentUserId == targetUserId)
            {
                _logger.LogWarning("User {UserId} attempted to follow themselves", currentUserId);
                throw new ArgumentException("Cannot follow yourself");
            }

            var currentUser = await _context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            var targetUser = await _context.Users.FindAsync(targetUserId);

            if (currentUser == null || targetUser == null)
            {
                _logger.LogWarning("Either current user {CurrentUserId} or target user {TargetUserId} not found during follow operation", 
                    currentUserId, targetUserId);
                throw new ArgumentException("User not found");
            }

            if (!currentUser.Following.Any(u => u.Id == targetUserId))
            {
                currentUser.Following.Add(targetUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {CurrentUserId} successfully followed user {TargetUserId}", currentUserId, targetUserId);
            }
            else
            {
                _logger.LogDebug("User {CurrentUserId} already follows user {TargetUserId}", currentUserId, targetUserId);
            }
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Error when user {CurrentUserId} tried to follow user {TargetUserId}", currentUserId, targetUserId);
            throw;
        }
    }

    /**
     * Unfollows a user.
     */
    public async Task UnFollowUserAsync(string currentUserId, string targetUserId)
    {
        _logger.LogInformation("User {CurrentUserId} is attempting to unfollow user {TargetUserId}", currentUserId, targetUserId);
        try
        {
            var currentUser = await _context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                _logger.LogWarning("User with ID {CurrentUserId} not found during unfollow operation", currentUserId);
                throw new ArgumentException($"User with ID {currentUserId} not found");
            }

            var targetUser = currentUser.Following.FirstOrDefault(u => u.Id == targetUserId);

            if (targetUser != null)
            {
                currentUser.Following.Remove(targetUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {CurrentUserId} successfully unfollowed user {TargetUserId}", currentUserId, targetUserId);
            }
            else
            {
                _logger.LogDebug("User {CurrentUserId} was not following user {TargetUserId}", currentUserId, targetUserId);
            }
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Error when user {CurrentUserId} tried to unfollow user {TargetUserId}", currentUserId, targetUserId);
            throw;
        }
    }
}
