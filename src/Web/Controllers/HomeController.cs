using Microsoft.AspNetCore.Mvc;
using MicroBlog.Web.Models;
using MicroBlog.Web.Services;
using Microsoft.Extensions.Logging;
using MicroBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

/**
 * Controller for handling home page and timeline functionality.
 */
namespace MicroBlog.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IPostService _postService;
    //private readonly IUserService _userService;
    //private readonly ILogger<HomeController> _logger;

    public HomeController(
        IPostService postService,
        IUserService userService,
        ILogger<HomeController> logger)
    {
        _postService = postService;
       // _userService = userService;
        //_logger = logger;
    }

    /**
     * Displays the home page with the user's timeline.
     */
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var timelinePosts = await _postService.GetTimelinePostsAsync(userId);
        return View(timelinePosts);
    }

    /**
     * Displays the create post page.
     */
    [Authorize]
    public IActionResult CreatePost()
    {
        return View();
    }

    /**
     * Handles post creation.
     */
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost(CreatePostViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Ensure we don't pass null to the service by providing empty string for null text
        string text = model.Text ?? string.Empty;
        // The IFormFile image parameter can be null, the service must handle this case
        await _postService.CreatePostAsync(userId, text, model.Image!);
        return RedirectToAction("Index");
    }
}
