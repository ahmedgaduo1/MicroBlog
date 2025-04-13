using System.ComponentModel.DataAnnotations;

namespace MicroBlog.Web.Models;

public class CommentViewModel
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(280, ErrorMessage = "Comment must be at most 280 characters")]
    public string? Text { get; set; }
    
    public int PostId { get; set; }
    
    public string? UserId { get; set; }
    
    public string? UserName { get; set; }
    
    public string? UserProfileImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public class CommentCreateModel
{
    public int PostId { get; set; }
    
    [Required]
    [StringLength(280, ErrorMessage = "Comment must be at most 280 characters")]
    public string? Text { get; set; }
}
