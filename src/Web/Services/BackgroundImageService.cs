using Hangfire;
using Microsoft.EntityFrameworkCore;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Application.Common.Interfaces;
using System.Text.RegularExpressions;

namespace MicroBlog.Web.Services;

/// <summary>
/// Service for handling background image processing tasks
/// </summary>
public class BackgroundImageService : IBackgroundImageService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<BackgroundImageService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _localImagesPath;
    
    public BackgroundImageService(
        ApplicationDbContext context,
        IBlobStorageService blobService,
        ILogger<BackgroundImageService> logger,
        IWebHostEnvironment environment)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        
        // Set up the local images directory path
        _localImagesPath = Path.Combine(_environment.WebRootPath, "uploads", "images");
    }
    
    /// <summary>
    /// Process locally stored images and move them to blob storage
    /// </summary>
    /// <returns>The number of images processed</returns>
    public async Task<int> ProcessLocalImagesToBlob()
    {
        int processedCount = 0;
        
        try
        {
            _logger.LogInformation("Starting local image to blob storage migration job");
            
            // First, check if blob storage is working
            if (!await IsBlobStorageAccessible())
            {
                _logger.LogWarning("Blob storage is not accessible. Skipping image migration.");
                return 0;
            }
            
            // Check if local directory exists
            if (!Directory.Exists(_localImagesPath))
            {
                _logger.LogInformation("Local images directory not found: {Path}", _localImagesPath);
                return 0;
            }
            
            // Get all posts that have local image URLs but no blob URL
            var postsWithLocalImages = await _context.Posts
                .Where(p => p.ImageUrl != null && 
                       p.ImageUrl.StartsWith("/uploads/images/") && 
                       (p.BlobImageUrl == null || p.BlobImageUrl == ""))
                .ToListAsync();
                
            _logger.LogInformation("Found {Count} posts with local images to process", postsWithLocalImages.Count);
            
            foreach (var post in postsWithLocalImages)
            {
                try
                {
                    // Extract filename from ImageUrl
                    string fileName = Path.GetFileName(post.ImageUrl);
                    string localFilePath = Path.Combine(_localImagesPath, fileName);
                    
                    // Check if file exists locally
                    if (!File.Exists(localFilePath))
                    {
                        _logger.LogWarning("Local image not found for post {PostId}: {FilePath}", post.Id, localFilePath);
                        continue;
                    }
                    
                    // Generate a unique blob name (use post ID and original filename)
                    string blobName = $"post-{post.Id}/{fileName}";
                    
                    // Upload to blob storage
                    using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var blobUrl = await _blobService.UploadAsync(fileStream, blobName, "image/jpeg");
                        
                        // Update post with blob URL
                        post.BlobImageUrl = blobUrl;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Successfully migrated image for post {PostId} to blob storage: {BlobUrl}", 
                            post.Id, blobUrl);
                            
                        processedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing image for post {PostId}", post.Id);
                }
            }
            
            _logger.LogInformation("Completed local image to blob storage migration job. Processed {Count} images", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessLocalImagesToBlob job");
            throw;
        }
    }
    
    /// <summary>
    /// Check if blob storage is accessible
    /// </summary>
    /// <returns>True if blob storage is working, false otherwise</returns>
    private async Task<bool> IsBlobStorageAccessible()
    {
        try
        {
            // Create a test blob name
            string testBlobName = $"test-connection-{Guid.NewGuid()}.txt";
            
            // Create a small test stream
            using (var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Blob storage connection test")))
            {
                // Try to upload a small test file
                await _blobService.UploadAsync(testStream, testBlobName, "text/plain");
                
                // If upload succeeds, try to delete the test blob
                await _blobService.DeleteAsync(testBlobName);
                
                _logger.LogInformation("Blob storage connection test successful");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob storage connection test failed");
            return false;
        }
    }
}
