/**
 * View model for displaying a post in the timeline.
 */

namespace MicroBlog.Web.Models;
public class PostViewModel
{
    public int Id { get; set; }
    public string? Text { get; set; }
    public string? ImageUrl { get; set; }
    public string? UserName { get; set; }
    public string? UserProfileImageUrl { get; set; }
    public DateTime PostedAt { get; set; }
    
    // Property for test compatibility - maps to PostedAt
    public DateTime CreatedAt
    {
        get { return PostedAt; }
        set { PostedAt = value; }
    }
    public int LikeCount { get; set; }
    public bool IsLiked { get; set; }
    public int CommentCount { get; set; }

    /// <summary>
    /// URL of the image to display, prioritizing processed images
    /// </summary>
    public string? DisplayImageUrl 
    { 
        get 
        {
            // Prioritize processed images
            if (ImageProcessingComplete && !string.IsNullOrEmpty(WebPImageUrl))
                return WebPImageUrl;
            
            // Fallback to original image
            return ImageUrl;
        }
    }

    /// <summary>
    /// URL to the processed WebP image
    /// </summary>
    public string? WebPImageUrl { get; set; }

    /// <summary>
    /// URLs for different image sizes
    /// </summary>
    public string? SmallImageUrl { get; set; }
    public string? MediumImageUrl { get; set; }
    public string? LargeImageUrl { get; set; }

    /// <summary>
    /// Flag indicating if image processing is complete
    /// </summary>
    public bool ImageProcessingComplete { get; set; }

    /// <summary>
    /// Geographic latitude of the post
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Geographic longitude of the post
    /// </summary>
    public double Longitude { get; set; }
}
