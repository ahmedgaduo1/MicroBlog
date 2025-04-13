using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Entities;
using MicroBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace MicroBlog.Web.Services;

public class ImageProcessingService : MicroBlog.Web.Services.IImageProcessingService, MicroBlog.Application.Common.Interfaces.IImageProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly IWebHostEnvironment _environment;

    // Image size configurations
    private static readonly int[] ImageWidths = { 320, 640, 1024 };

    public ImageProcessingService(
        ApplicationDbContext context,
        IBlobStorageService blobStorageService,
        ILogger<ImageProcessingService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _logger = logger;
        _environment = environment;
    }

    public async Task ProcessImageAsync(int postId, string originalImageUrl)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for image processing", postId);
                return;
            }

            // Convert URL to file path (assuming local storage for this implementation)
            string originalImagePath = Path.Combine(_environment.WebRootPath, originalImageUrl.TrimStart('/'));
            
            if (!File.Exists(originalImagePath))
            {
                _logger.LogWarning("Image file not found at {Path}", originalImagePath);
                return;
            }

            // Process and upload images in different sizes
            using (var image = SixLabors.ImageSharp.Image.Load(originalImagePath))
            {
                // Generate WebP versions
                var processedImages = await GenerateImageVariants(image, postId);

                // Update post with processed image URLs
                post.WebPImageUrl = processedImages[ImageSize.Medium];
                post.SmallImageUrl = processedImages[ImageSize.Small];
                post.LargeImageUrl = processedImages[ImageSize.Large];
                post.ImageProcessingComplete = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed images for post {PostId}", postId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for post {PostId}", postId);
            throw;
        }
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Empty image file");
            return false;
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Check file extension
        if (!allowedExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("Unsupported image format: {Extension}", fileExtension);
            return false;
        }

        // Check file size (2MB limit)
        const long maxFileSize = 2 * 1024 * 1024; // 2MB
        if (file.Length > maxFileSize)
        {
            _logger.LogWarning("Image exceeds maximum file size of 2MB");
            return false;
        }

        return true;
    }

    private enum ImageSize { Small, Medium, Large }

    private async Task<Dictionary<ImageSize, string>> GenerateImageVariants(SixLabors.ImageSharp.Image image, int postId)
    {
        var processedImages = new Dictionary<ImageSize, string>();

        foreach (var size in Enum.GetValues(typeof(ImageSize)).Cast<ImageSize>())
        {
            int width = GetWidthForSize(size);
            string fileName = $"post-{postId}-{size.ToString().ToLower()}.webp";

            // Resize image maintaining aspect ratio
            using (var resizedImage = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(width, 0), // Maintain aspect ratio
                Mode = ResizeMode.Max
            })))
            {
                // Save to memory stream
                using (var memoryStream = new MemoryStream())
                {
                    await resizedImage.SaveAsync(memoryStream, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder());
                    memoryStream.Position = 0;

                    // Upload to blob storage
                    var blobUrl = await _blobStorageService.UploadAsync(memoryStream, fileName, "image/webp");
                    processedImages[size] = blobUrl;
                }
            }
        }

        return processedImages;
    }

    private int GetWidthForSize(ImageSize size)
    {
        return size switch
        {
            ImageSize.Small => ImageWidths[0],
            ImageSize.Medium => ImageWidths[1],
            ImageSize.Large => ImageWidths[2],
            _ => throw new ArgumentOutOfRangeException(nameof(size), "Invalid image size")
        };
    }

    public async Task<string> SaveOriginalImageAsync(IFormFile image)
    {
        try
        {
            if (!IsValidImage(image))
            {
                throw new ArgumentException("Invalid image file");
            }

            // Save original image
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);
            
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Return the URL path to the image
            return $"/uploads/profiles/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving profile image");
            throw;
        }
    }

    public async Task<(int width, int height)> GetImageDimensionsAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid image file");
            }

            using (var stream = file.OpenReadStream())
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(stream))
            {
                return (image.Width, image.Height);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image dimensions");
            throw;
        }
    }
}
