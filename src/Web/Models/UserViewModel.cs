namespace MicroBlog.Web.Models;

/**
 * View model for displaying a user in search results or user suggestions.
 */
public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsFollowed { get; set; }
}
