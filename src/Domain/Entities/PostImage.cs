namespace MicroBlog.Domain.Entities;

/**
 * Represents a processed image variation for a post.
 * Each PostImage contains a specific size variation of the original image,
 * optimized for different display scenarios (mobile, tablet, desktop).
 */
public class PostImage : BaseEntity
{
    /**
     * URL to the processed image (WebP format)
     */
    public required string Url { get; set; }

    /**
     * Width of the processed image in pixels
     */
    public int Width { get; set; }

    /**
     * Height of the processed image in pixels
     */
    public int Height { get; set; }

    /**
     * Image format (default: webp)
     */
    public string Format { get; set; } = "webp";

    /**
     * ID of the associated post
     */
    public int PostId { get; set; }

    /**
     * Navigation property to the associated post
     */
    public Post Post { get; set; } = null!;
}
