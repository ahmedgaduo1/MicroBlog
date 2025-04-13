using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Identity;
using MicroBlog.Web.Models;

namespace MicroBlog.Web.Controllers;

public class AccountController : Controller
{
    private readonly IIdentityService _identityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IIdentityService identityService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _identityService = identityService;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // First authenticate with JWT for API access
        var (result, token) = await _identityService.AuthenticateAsync(model.Username, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View(model);
        }

        // Store the JWT token in a cookie for API authentication
        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.Now.AddHours(1)
        });

        // Now sign in with ASP.NET Core Identity for cookie authentication
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, model.RememberMe);
        }

        _logger.LogInformation("User {Username} logged in successfully", model.Username);

        return LocalRedirect(returnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (result, userId) = await _identityService.CreateUserAsync(model.Username, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View(model);
        }

        _logger.LogInformation("User {Username} registered successfully", model.Username);

        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Remove the JWT token cookie
        Response.Cookies.Delete("AuthToken");
        
        // Sign out of ASP.NET Core Identity
        await _signInManager.SignOutAsync();
        
        _logger.LogInformation("User logged out");
        return RedirectToAction("Login");
    }
}
