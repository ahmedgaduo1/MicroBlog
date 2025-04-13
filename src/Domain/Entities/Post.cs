using MicroBlog.Domain.Identity;

namespace MicroBlog.Domain.Entities;

/**
 * Represents a microblog post in the system.
 * Posts can contain text (max 140 characters) and optionally include images.
 * Each post is associated with a user and includes geographic coordinates.
 */
public class Post : BaseAuditableEntity
{
    /**
     * The text content of the post (maximum 140 characters)
     */
    public required string Text { get; set; }

    /**
     * URL to the processed image (WebP format)
     */
    public string? ImageUrl { get; set; }

    /**
     * URL to the original uploaded image
     */
    public string? OriginalImageUrl { get; set; }
    
    /**
     * URL to the image stored in blob storage
     */
    public string? BlobImageUrl { get; set; }

    /**
     * URL to the processed WebP image (optimized)
     */
    public string? WebPImageUrl { get; set; }

    /**
     * URLs for different image sizes
     */
    public string? SmallImageUrl { get; set; }
    public string? MediumImageUrl { get; set; }
    public string? LargeImageUrl { get; set; }

    /**
     * Flag indicating if image processing is complete
     */
    public bool ImageProcessingComplete { get; set; } = false;

    /**
     * Geographic latitude of the post (randomly generated)
     */
    public double Latitude { get; set; }

    /**
     * Geographic longitude of the post (randomly generated)
     */
    public double Longitude { get; set; }

    /**
     * ID of the user who created the post
     */
    public required string UserId { get; set; }

    /**
     * Username of the post creator
     */
    public string? UserName { get; set; }
    
    /**
     * Navigation property for the post creator
     */
    public virtual ApplicationUser? User { get; set; }

    /**
     * Collection of users who liked this post
     */
    public virtual ICollection<ApplicationUser> LikedByUsers { get; set; } = new List<ApplicationUser>();
    
    /**
     * Collection of image variations for different display sizes
     */
    public ICollection<PostImage> Images { get; private set; } = new List<PostImage>();
    
    /**
     * Number of likes this post has received
     */
    public int LikeCount { get; set; }
    
    /**
     * Collection of comments on this post
     */
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    
    /**
     * Number of comments this post has received
     */
    public int CommentCount { get; set; }
}
