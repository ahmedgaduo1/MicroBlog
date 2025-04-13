using System.Drawing;
using System.Drawing.Imaging;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Application.Common.Exceptions;
using MicroBlog.Domain.Entities;
using MicroBlog.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MicroBlog.Infrastructure.Services;

/**
 * Service responsible for processing and managing post images.
 * Handles image validation, storage, and generation of responsive image variations.
 */
public class ImageProcessingService : IImageProcessingService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILocalStorageService _localStorageService;
    private readonly ILogger<ImageProcessingService> _logger;
    
    /**
     * Common dimensions for responsive images:
     * - 320x240: Small mobile devices
     * - 640x480: Mobile devices
     * - 1024x768: Tablets
     * - 1920x1080: Desktop displays
     */
    private readonly (int width, int height)[] _targetDimensions = new[]
    {
        (320, 240),   // Small mobile
        (640, 480),   // Mobile
        (1024, 768),  // Tablet
        (1920, 1080)  // Desktop
    };

    public ImageProcessingService(
        IBlobStorageService blobStorageService,
        IApplicationDbContext dbContext,
        ILocalStorageService localStorageService,
        ILogger<ImageProcessingService> logger)
    {
        _blobStorageService = blobStorageService;
        _dbContext = dbContext;
        _localStorageService = localStorageService;
        _logger = logger;
    }

    /**
     * Saves the original uploaded image to blob storage or local storage as fallback.
     * 
     * @param file The uploaded image file
     * @returns URL to the stored image or local file path if blob storage fails
     */
    public async Task<string> SaveOriginalImageAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var originalBlobName = await _blobStorageService.UploadAsync(stream, file.FileName, file.ContentType);
            return await _blobStorageService.GetBlobUrlAsync(originalBlobName);
        }
        catch (BlobStorageException ex)
        {
            _logger.LogWarning(ex, "Blob storage unavailable, storing image locally");
            
            // Store image locally as fallback
            await using var stream = file.OpenReadStream();
            var fileBytes = new byte[file.Length];
            await stream.ReadExactlyAsync(fileBytes, 0, fileBytes.Length);
            var localPath = await _localStorageService.SaveFileAsync(fileBytes, file.FileName, file.ContentType);
            return localPath;
        }
    }

    /**
     * Validates if an uploaded file is a valid image.
     * 
     * @param file The uploaded image file
     * @returns True if the file is a valid image, false otherwise
     */
    public bool IsValidImage(IFormFile file)
    {
        // Check file size (2MB max)
        if (file.Length > 2 * 1024 * 1024)
            return false;

        // Check file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        
        return allowedExtensions.Contains(extension);
    }

    /**
     * Gets the dimensions of an image.
     * 
     * @param file The image file
     * @returns Tuple containing width and height of the image
     */
    public async Task<(int width, int height)> GetImageDimensionsAsync(IFormFile file)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(file.OpenReadStream());
        return (image.Width, image.Height);
    }

    /**
     * Processes an image by generating responsive variations and storing them.
     * If blob storage fails, stores images locally as fallback.
     * 
     * @param postId The ID of the post associated with the image
     * @param originalImageUrl URL to the original uploaded image
     */
    public async Task ProcessImageAsync(int postId, string originalImageUrl)
    {
        try
        {
            var post = await _dbContext.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for image processing", postId);
                return;
            }

            // Extract the blob name from the URL
            var originalBlobName = Path.GetFileName(new Uri(originalImageUrl).LocalPath);
            
            // Download the original image
            Stream originalImageStream;
            try
            {
                originalImageStream = await _blobStorageService.DownloadAsync(originalBlobName);
            }
            catch (BlobStorageException ex)
            {
                _logger.LogWarning(ex, "Blob storage unavailable, using local storage for image processing");
                // Extract the local file path from the URL and read the file directly
                var fileName = Path.GetFileName(originalImageUrl);
                var filePath = Path.Combine(
                    Path.Combine(Directory.GetCurrentDirectory(), "local-storage"), 
                    fileName);
                originalImageStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }

            using (originalImageStream)
            {
                // Load the image with ImageSharp
                using var image = await SixLabors.ImageSharp.Image.LoadAsync(originalImageStream);
                
                // Process for each target dimension
                foreach (var dimension in _targetDimensions)
                {
                    await using var resizedImageStream = new MemoryStream();
                    
                    // Clone the image and resize it
                    using var resizedImage = image.Clone(ctx => 
                    {
                        ctx.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(dimension.width, dimension.height),
                            Mode = ResizeMode.Max
                        });
                    });
                    
                    // Save as WebP format with high quality
                    await resizedImage.SaveAsWebpAsync(resizedImageStream, new WebpEncoder { Quality = 80 });
                    resizedImageStream.Position = 0;

                    try
                    {
                        // Try to store in blob storage first
                        var blobName = await _blobStorageService.UploadAsync(
                            resizedImageStream, 
                            $"{Path.GetFileNameWithoutExtension(originalBlobName)}_{dimension.width}x{dimension.height}.webp", 
                            "image/webp");
                        
                        var imageUrl = await _blobStorageService.GetBlobUrlAsync(blobName);
                        
                        post.Images.Add(new PostImage
                        {
                            Url = imageUrl,
                            Width = dimension.width,
                            Height = dimension.height,
                            Format = "webp",
                            PostId = post.Id
                        });
                    }
                    catch (BlobStorageException ex)
                    {
                        _logger.LogWarning(ex, "Blob storage unavailable, storing resized image locally");
                        
                        // Store locally as fallback
                        var localPath = await _localStorageService.SaveFileAsync(
                            resizedImageStream.ToArray(),
                            $"{Path.GetFileNameWithoutExtension(originalBlobName)}_{dimension.width}x{dimension.height}.webp",
                            "image/webp");
                        
                        post.Images.Add(new PostImage
                        {
                            Url = localPath,
                            Width = dimension.width,
                            Height = dimension.height,
                            Format = "webp",
                            PostId = post.Id
                        });
                    }
                }
                
                // Mark the post as processed
                post.ImageProcessingComplete = true;
                
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                _logger.LogInformation("Successfully processed images for post {PostId}", postId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for post {PostId}", postId);
        }
    }
}
