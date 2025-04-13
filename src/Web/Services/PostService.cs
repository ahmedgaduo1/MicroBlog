// Ignore Spelling: Accessor

using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Entities;
using MicroBlog.Domain.Identity;
using MicroBlog.Web.Models;
using Microsoft.EntityFrameworkCore;
using Hangfire;

/**
 * Service for handling post-related operations.
 */
namespace MicroBlog.Web.Services;
public class PostService : IPostService
{
    private readonly IApplicationDbContext _context;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<PostService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostService(
        IApplicationDbContext context,
        IImageProcessingService imageProcessingService,
        ILogger<PostService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _imageProcessingService = imageProcessingService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /**
     * Gets the user's timeline posts (posts from followed users).
     */
    public async Task<IEnumerable<PostViewModel>> GetTimelinePostsAsync(string userId)
    {
        _logger.LogInformation("Getting timeline posts for user {UserId}", userId);
        try
        {
            // Use a raw SQL query to select only the columns that exist in the database
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50) // Limit to 50 posts for performance
                .AsNoTracking() // For better performance
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} posts for user {UserId}'s timeline", posts.Count, userId);
            
            return posts.Select(p => new PostViewModel
            {
                Id = p.Id,
                Text = p.Text,
                ImageUrl = p.ImageUrl,
                UserName = p.UserName,
                UserProfileImageUrl = p.User?.ProfileImageUrl,
                PostedAt = p.CreatedAt,
                LikeCount = p.LikeCount,
                IsLiked = p.LikedByUsers.Any(u => u.Id == userId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timeline posts for user {UserId}", userId);
            throw;
        }
    }

    /**
     * Creates a new post with optional image.
     */
    public async Task CreatePostAsync(string userId, string text, IFormFile? image)
    {
        // Validate text length
        if (string.IsNullOrWhiteSpace(text) || text.Length > 140)
        {
            throw new ArgumentException("Post text must be between 1 and 140 characters.");
        }

        // Generate random coordinates
        var (latitude, longitude) = GenerateRandomCoordinates();

        var post = new Post
        {
            Text = text,
            UserId = userId,
            UserName = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(),
            CreatedAt = DateTime.UtcNow,
            Latitude = latitude,
            Longitude = longitude
        };

        // Handle image upload
        if (image != null && image.Length > 0)
        {
            // Validate image
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Only JPG, PNG, and WebP images are allowed.");
            }

            // Check file size (2MB limit)
            if (image.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException("Image must be less than 2MB.");
            }

            // Save original image
            var uploadsFolder = Path.Combine(_httpContextAccessor.HttpContext.RequestServices.GetService<IWebHostEnvironment>().WebRootPath, "uploads", "images");
            Directory.CreateDirectory(uploadsFolder);
            
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Set image URL
            post.ImageUrl = $"/uploads/images/{uniqueFileName}";
            post.OriginalImageUrl = post.ImageUrl;
            post.ImageProcessingComplete = false;

            // Enqueue image processing job
            BackgroundJob.Enqueue<IImageProcessingService>(
                x => x.ProcessImageAsync(post.Id, filePath)
            );
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post created successfully for user {UserId}", userId);
    }

    /**
     * Likes a post.
     */
    public async Task LikePostAsync(string userId, int postId)
    {
        _logger.LogInformation("User {UserId} is liking post {PostId}", userId, postId);
        try
        {
            var post = await _context.Posts
                .Include(p => p.LikedByUsers)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post != null && !post.LikedByUsers.Any(u => u.Id == userId))
            {
                post.LikedByUsers.Add(new ApplicationUser { Id = userId });
                await _context.SaveChangesAsync();
                _logger.LogDebug("User {UserId} successfully liked post {PostId}", userId, postId);
            }
            else
            {
                _logger.LogDebug("Post {PostId} not found or already liked by user {UserId}", postId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when user {UserId} tried to like post {PostId}", userId, postId);
            throw;
        }
    }

    /**
     * Unlikes a post.
     */
    public async Task UnlikePostAsync(string userId, int postId)
    {
        _logger.LogInformation("User {UserId} is unliking post {PostId}", userId, postId);
        try
        {
            var post = await _context.Posts
                .Include(p => p.LikedByUsers)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post != null)
            {
                var user = post.LikedByUsers.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    post.LikedByUsers.Remove(user);
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("User {UserId} successfully unliked post {PostId}", userId, postId);
                }
                else
                {
                    _logger.LogDebug("Post {PostId} was not liked by user {UserId}", postId, userId);
                }
            }
            else
            {
                _logger.LogDebug("Post {PostId} not found when user {UserId} tried to unlike it", postId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when user {UserId} tried to unlike post {PostId}", userId, postId);
            throw;
        }
    }

    /**
     * Gets all posts for a specific user.
     */
    public async Task<IEnumerable<PostViewModel>> GetUserPostsAsync(string userId)
    {
        _logger.LogInformation("Getting posts for user {UserId}", userId);
        try
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} posts for user {UserId}", posts.Count, userId);
            
            return posts.Select(p => new PostViewModel
            {
                Id = p.Id,
                Text = p.Text,
                ImageUrl = p.ImageUrl,
                UserName = p.UserName,
                UserProfileImageUrl = p.User?.ProfileImageUrl,
                PostedAt = p.CreatedAt,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts for user {UserId}", userId);
            throw;
        }
    }

    /**
     * Gets comments for a specific post.
     */
    public async Task<IEnumerable<CommentViewModel>> GetCommentsForPostAsync(int postId)
    {
        _logger.LogInformation("Getting comments for post {PostId}", postId);
        try
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found when trying to get comments", postId);
                return new List<CommentViewModel>();
            }

            var comments = post.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Text = c.Text,
                    PostId = c.PostId,
                    UserId = c.UserId,
                    UserName = c.UserName,
                    UserProfileImageUrl = c.User?.ProfileImageUrl,
                    CreatedAt = c.CreatedAt
                });

            _logger.LogDebug("Retrieved {Count} comments for post {PostId}", comments.Count(), postId);
            return comments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comments for post {PostId}", postId);
            throw;
        }
    }

    /**
     * Adds a comment to a post.
     */
    public async Task<CommentViewModel> AddCommentAsync(string userId, int postId, string text)
    {
        _logger.LogInformation("Adding comment to post {PostId} by user {UserId}", postId, userId);
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when adding comment", userId);
                throw new InvalidOperationException("User not found");
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found when adding comment", postId);
                throw new InvalidOperationException("Post not found");
            }

            var comment = new MicroBlog.Domain.Entities.Comment
            {
                Text = text,
                PostId = postId,
                UserId = userId,
                UserName = user.UserName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<MicroBlog.Domain.Entities.Comment>().Add(comment);
            
            // Update comment count on post
            post.CommentCount++;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully added comment {CommentId} to post {PostId}", comment.Id, postId);

            return new CommentViewModel
            {
                Id = comment.Id,
                Text = comment.Text,
                PostId = comment.PostId,
                UserId = comment.UserId,
                UserName = comment.UserName,
                UserProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = comment.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to post {PostId} by user {UserId}", postId, userId);
            throw;
        }
    }

    /**
     * Deletes a comment.
     */
    public async Task DeleteCommentAsync(string userId, int commentId)
    {
        _logger.LogInformation("Deleting comment {CommentId} by user {UserId}", commentId, userId);
        try
        {
            var comment = await _context.Set<MicroBlog.Domain.Entities.Comment>()
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return;
            }

            // Only allow deletion by the comment author or the post owner
            if (comment.UserId != userId)
            {
                var post = await _context.Posts.FindAsync(comment.PostId);
                if (post == null || post.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} not authorized to delete comment {CommentId}", userId, commentId);
                    throw new UnauthorizedAccessException("Not authorized to delete this comment");
                }
            }

            _context.Set<MicroBlog.Domain.Entities.Comment>().Remove(comment);
            
            // Update comment count on post
            var parentPost = await _context.Posts.FindAsync(comment.PostId);
            if (parentPost != null && parentPost.CommentCount > 0)
            {
                parentPost.CommentCount--;
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted comment {CommentId}", commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId} by user {UserId}", commentId, userId);
            throw;
        }
    }

    /// <summary>
    /// Generates random geographic coordinates
    /// </summary>
    /// <returns>A tuple of (latitude, longitude)</returns>
    private (double Latitude, double Longitude) GenerateRandomCoordinates()
    {
        // Rough geographic bounds (e.g., global range)
        const double minLatitude = -90.0;
        const double maxLatitude = 90.0;
        const double minLongitude = -180.0;
        const double maxLongitude = 180.0;

        var random = new Random();
        
        double latitude = random.NextDouble() * (maxLatitude - minLatitude) + minLatitude;
        double longitude = random.NextDouble() * (maxLongitude - minLongitude) + minLongitude;

        return (Math.Round(latitude, 6), Math.Round(longitude, 6));
    }
}
