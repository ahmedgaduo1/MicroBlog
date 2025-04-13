using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MicroBlog.Web.Models;
using MicroBlog.Web.Services;
using MicroBlog.Web.Extensions;

namespace MicroBlog.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<PostApiController> _logger;

    public PostApiController(IPostService postService, ILogger<PostApiController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    [HttpPost("like/{postId}")]
    public async Task<IActionResult> LikePost(int postId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _postService.LikePostAsync(userId, postId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking post {PostId}", postId);
            return StatusCode(500, new { success = false, message = "Error processing like" });
        }
    }

    [HttpPost("unlike/{postId}")]
    public async Task<IActionResult> UnlikePost(int postId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _postService.UnlikePostAsync(userId, postId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unliking post {PostId}", postId);
            return StatusCode(500, new { success = false, message = "Error processing unlike" });
        }
    }

    [HttpGet("{postId}/likes")]
    public async Task<IActionResult> GetLikes(int postId)
    {
        try
        {
            // This would typically return like count and if the current user liked the post
            // For simplicity, we're just returning a success response
            return Ok(new { success = true, likes = 5 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting likes for post {PostId}", postId);
            return StatusCode(500, new { success = false, message = "Error fetching likes" });
        }
    }
}
