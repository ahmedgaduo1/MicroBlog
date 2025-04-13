using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Application.Common.Models;
using MicroBlog.Domain.Entities;
using MediatR;
using System.Security;
using MicroBlog.Application.Common.Exceptions;

namespace MicroBlog.Application.Posts.Commands.CreatePost;

/**
 * Handler for creating new microblog posts.
 * This handler manages the creation of posts with text and optional images,
 * including image validation and processing.
 */
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<CreatePostCommandHandler> _logger;
    private readonly Random _random = new();

    /**
     * Initializes a new instance of the CreatePostCommandHandler class.
     * 
     * @param context The application database context
     * @param imageProcessingService The image processing service
     * @param currentUserService The current user service
     * @param identityService The identity service
     * @param logger The logger
     */
    public CreatePostCommandHandler(
        IApplicationDbContext context,
        IImageProcessingService imageProcessingService,
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        ILogger<CreatePostCommandHandler> logger)
    {
        _context = context;
        _imageProcessingService = imageProcessingService;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    /**
     * Handles the creation of a new post.
     * 
     * @param request The create post command containing post text and optional image
     * @param cancellationToken Token for canceling the operation
     * @returns The ID of the newly created post
     * @throws ApplicationException If post text exceeds 140 characters
     * @throws UnauthorizedAccessException If user is not authenticated
     */
    public async Task<int> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        // Validate post text is within the 140 character limit
        if (request.Text.Length > 140)
        {
            throw new ApplicationException("Post text exceeds the maximum length of 140 characters");
        }

        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated");
        
        var userName = await _identityService.GetUserNameAsync(userId);

        // Generate random geographic coordinates
        var latitude = _random.NextDouble() * 180 - 90;  // Range: -90 to 90
        var longitude = _random.NextDouble() * 360 - 180; // Range: -180 to 180

        var post = new Post
        {
            Text = request.Text,
            UserId = userId,
            UserName = userName,
            Latitude = latitude,
            Longitude = longitude,
            ImageProcessingComplete = false
        };

        // Handle image upload if present
        if (request.Image != null)
        {
            // Validate image
            if (!_imageProcessingService.IsValidImage(request.Image))
            {
                throw new ApplicationException("Invalid image format or size. Only JPG, PNG, and WebP formats are allowed with a maximum size of 2MB.");
            }

            try
            {
                // Save original image
                post.OriginalImageUrl = await _imageProcessingService.SaveOriginalImageAsync(request.Image);
                post.ImageUrl = post.OriginalImageUrl; // Initially use original image
            }
            catch (BlobStorageException ex)
            {
                _logger.LogWarning(ex, "Blob storage unavailable, post created without image");
                post.ImageUrl = null;
                post.OriginalImageUrl = null;
            }
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        // If image was uploaded successfully, trigger background processing
        if (request.Image != null && post.OriginalImageUrl != null)
        {
            _logger.LogInformation("Post {PostId} created with image, scheduled for processing", post.Id);
        }

        return post.Id;
    }
}
