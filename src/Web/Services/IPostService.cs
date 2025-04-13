
using MicroBlog.Web.Models;

namespace MicroBlog.Web.Services;
/**
 * Interface for post-related operations.
 */
public interface IPostService
{
    Task<IEnumerable<PostViewModel>> GetTimelinePostsAsync(string userId);
    Task CreatePostAsync(string userId, string text, IFormFile image);
    Task LikePostAsync(string userId, int postId);
    Task UnlikePostAsync(string userId, int postId);
    Task<IEnumerable<PostViewModel>> GetUserPostsAsync(string userId);
    
    // Comment-related methods
    Task<IEnumerable<CommentViewModel>> GetCommentsForPostAsync(int postId);
    Task<CommentViewModel> AddCommentAsync(string userId, int postId, string text);
    Task DeleteCommentAsync(string userId, int commentId);
}
