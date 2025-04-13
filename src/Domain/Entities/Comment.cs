using MicroBlog.Domain.Identity;

namespace MicroBlog.Domain.Entities;

/**
 * Represents a comment on a MicroBlog post.
 */
public class Comment : BaseAuditableEntity
{
    /**
     * The text content of the comment (maximum 280 characters)
     */
    public required string Text { get; set; }
    
    /**
     * ID of the post this comment belongs to
     */
    public int PostId { get; set; }
    
    /**
     * Navigation property to the post
     */
    public virtual Post? Post { get; set; }
    
    /**
     * ID of the user who created the comment
     */
    public required string UserId { get; set; }
    
    /**
     * Username of the comment creator
     */
    public string? UserName { get; set; }
    
    /**
     * Navigation property for the comment creator
     */
    public virtual ApplicationUser? User { get; set; }
}
