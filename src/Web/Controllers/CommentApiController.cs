using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MicroBlog.Web.Models;
using MicroBlog.Web.Services;
using MicroBlog.Web.Extensions;

namespace MicroBlog.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<CommentApiController> _logger;

    public CommentApiController(IPostService postService, ILogger<CommentApiController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetComments(int postId)
    {
        try
        {
            var comments = await _postService.GetCommentsForPostAsync(postId);
            return Ok(new { success = true, comments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return StatusCode(500, new { success = false, message = "Error fetching comments" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] CommentCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var comment = await _postService.AddCommentAsync(userId, model.PostId, model.Text);
            return Ok(new { success = true, comment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to post {PostId}", model.PostId);
            return StatusCode(500, new { success = false, message = "Error adding comment" });
        }
    }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _postService.DeleteCommentAsync(userId, commentId);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return StatusCode(500, new { success = false, message = "Error deleting comment" });
        }
    }
}
