using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

/**
 * View model for creating a new post.
 */

namespace MicroBlog.Web.Models;
public class CreatePostViewModel
{
    [Required]
    [StringLength(140, ErrorMessage = "The post text must be at most 140 characters.")]
    public string? Text { get; set; }

    public IFormFile? Image { get; set; }
}
