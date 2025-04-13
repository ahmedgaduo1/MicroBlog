namespace MicroBlog.Web.Models;

/**
 * View model for displaying a user's profile.
 */
public class UserProfileViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowedByCurrentUser { get; set; }
    public IEnumerable<PostViewModel> Posts { get; set; } = new List<PostViewModel>();
}
