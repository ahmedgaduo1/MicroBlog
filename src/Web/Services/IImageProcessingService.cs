using MicroBlog.Domain.Entities;

namespace MicroBlog.Web.Services;

public interface IImageProcessingService
{
    /// <summary>
    /// Process an uploaded image for a post
    /// </summary>
    /// <param name="postId">ID of the post</param>
    /// <param name="originalImageUrl">URL or path to the original uploaded image</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ProcessImageAsync(int postId, string originalImageUrl);
    
    /// <summary>
    /// Saves an original image and returns the URL
    /// </summary>
    /// <param name="image">The image file to save</param>
    /// <returns>The URL of the saved image</returns>
    Task<string> SaveOriginalImageAsync(IFormFile image);

    /// <summary>
    /// Validates if an image file meets requirements
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>True if the image is valid, false otherwise</returns>
    bool IsValidImage(IFormFile file);

    /// <summary>
    /// Gets the dimensions of an image file
    /// </summary>
    /// <param name="file">The image file</param>
    /// <returns>Tuple with width and height</returns>
    Task<(int width, int height)> GetImageDimensionsAsync(IFormFile file);
}
