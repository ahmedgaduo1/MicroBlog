namespace MicroBlog.Web.Services;

/// <summary>
/// Service interface for background image processing tasks
/// </summary>
public interface IBackgroundImageService
{
    /// <summary>
    /// Process locally stored images and move them to blob storage
    /// </summary>
    /// <returns>The number of images processed</returns>
    Task<int> ProcessLocalImagesToBlob();
}
