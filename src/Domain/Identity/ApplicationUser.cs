// Ignore Spelling: Bio

using Microsoft.AspNetCore.Identity;
using System;

namespace MicroBlog.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActive { get; set; }

    // Navigation properties for user relationships
    public virtual ICollection<ApplicationUser> Followers { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<ApplicationUser> Following { get; set; } = new List<ApplicationUser>();

    // Navigation property for a user's liked posts
    public virtual ICollection<Post> LikedPosts { get; set; } = new List<Post>();
   
    // Navigation property for a user's created posts
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();


}
