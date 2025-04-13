using MicroBlog.Application.Common.Models;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

/**
 * Service providing identity and authentication functionality.
 * Implements JWT-based authentication and user management operations.
 */

namespace MicroBlog.Infrastructure.Identity;
public class IdentityService :  IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly JwtSettings _jwtSettings;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _jwtSettings = jwtSettings.Value;
    }

    /**
     * Gets the user ID from a ClaimsPrincipal.
     *
     * @param user The claims principal
     * @returns The user ID
     */
    public Task<string> GetUserIdAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Task.FromResult(userId);
    }

    /**
     * Gets the username from a ClaimsPrincipal.
     *
     * @param user The claims principal
     * @returns The username
     */
    public async Task<string> GetUserNameAsync(ClaimsPrincipal user)
    {
        var userId = await GetUserIdAsync(user);
        var appUser = await _userManager.FindByIdAsync(userId);
        return appUser?.UserName ?? string.Empty;
    }

    /**
     * Gets the username for a given user ID.
     * 
     * @param userId The ID of the user
     * @returns The username if found, null otherwise
     */
    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    /**
     * Gets the username for a given user ID - alias of GetUserNameAsync for compatibility.
     * 
     * @param userId The ID of the user
     * @returns The username if found, null otherwise
     */
    public async Task<string?> GetUserNameByIdAsync(string userId)
    {
        return await GetUserNameAsync(userId);
    }

    /**
     * Creates a new user with the specified username and password.
     * 
     * @param userName The username for the new user
     * @param password The password for the new user
     * @returns A tuple containing the result and the user ID
     */
    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    /**
     * Checks if a user is in a specific role.
     * 
     * @param userId The ID of the user
     * @param role The role to check
     * @returns True if the user is in the specified role, false otherwise
     */
    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    /**
     * Checks if a user is authorized for a specific policy.
     * 
     * @param userId The ID of the user
     * @param policyName The name of the policy to check
     * @returns True if the user is authorized, false otherwise
     */
    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    /**
     * Deletes a user by ID.
     * 
     * @param userId The ID of the user to delete
     * @returns The result of the operation
     */
    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    /**
     * Deletes a user.
     * 
     * @param user The user to delete
     * @returns The result of the operation
     */
    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        await _userManager.DeleteAsync(user);

        return Result.Success();
    }

    /**
     * Authenticates a user with JWT.
     * 
     * @param userName The username to authenticate
     * @param password The password to authenticate
     * @returns A tuple containing the result and JWT token
     */
    public async Task<(Result Result, string Token)> AuthenticateAsync(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);
        
        if (user == null)
        {
            return (Result.Failure(new[] { "User not found" }), string.Empty);
        }
        
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        
        if (!passwordValid)
        {
            return (Result.Failure(new[] { "Invalid password" }), string.Empty);
        }
        
        var token = await GenerateJwtTokenAsync(user.Id, user.UserName!);
        
        return (Result.Success(), token);
    }

    /**
     * Generates a JWT token for a user.
     * 
     * @param userId The ID of the user
     * @param userName The username of the user
     * @returns The JWT token
     */
    public async Task<string> GenerateJwtTokenAsync(string userId, string userName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            throw new ArgumentNullException($"User with ID {userId} not found");
        }
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId)
        };
        
        var roles = await _userManager.GetRolesAsync(user);
        
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        
        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: expires,
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /**
     * Validates a JWT token.
     * 
     * @param token The JWT token to validate
     * @returns True if the token is valid, false otherwise
     */
    public Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(false);
        }
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
