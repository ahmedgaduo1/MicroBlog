using System.Net;
using System.Net.Http.Json;

namespace MicroBlog.Tests.Integration;

public class AuthenticationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_WithValidData_ShouldSucceed()
    {
        // Arrange
        var registrationModel = new 
        {
            UserName = $"newuser_{Guid.NewGuid():N}",
            Email = $"newuser_{Guid.NewGuid():N}@example.com",
            Password = "Test@Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/register", registrationModel);

        // Assert
        response.EnsureSuccessStatusCode();
        var registrationResult = await response.Content.ReadFromJsonAsync<dynamic>();
        
        registrationResult.Should().NotBeNull();
        ((string)registrationResult.userName).Should().Be(registrationModel.UserName);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var registrationModel = new 
        {
            UserName = $"loginuser_{Guid.NewGuid():N}",
            Email = $"loginuser_{Guid.NewGuid():N}@example.com",
            Password = "Test@Password123!"
        };

        // First, register the user
        var registrationResponse = await _client.PostAsJsonAsync("/api/account/register", registrationModel);
        registrationResponse.EnsureSuccessStatusCode();

        // Now attempt login
        var loginModel = new 
        {
            UserName = registrationModel.UserName,
            Password = registrationModel.Password
        };

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", loginModel);

        // Assert
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<dynamic>();
        
        loginResult.Should().NotBeNull();
        ((string)loginResult.token).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var loginModel = new 
        {
            UserName = "nonexistent_user",
            Password = "WrongPassword123!"
        };

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", loginModel);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithExistingUsername_ShouldFail()
    {
        // Arrange
        var existingUsername = $"duplicateuser_{Guid.NewGuid():N}";
        var registrationModel1 = new 
        {
            UserName = existingUsername,
            Email = $"user1_{Guid.NewGuid():N}@example.com",
            Password = "Test@Password123!"
        };

        var registrationModel2 = new 
        {
            UserName = existingUsername,
            Email = $"user2_{Guid.NewGuid():N}@example.com",
            Password = "AnotherTest@Password123!"
        };

        // First, register the initial user
        var firstRegistrationResponse = await _client.PostAsJsonAsync("/api/account/register", registrationModel1);
        firstRegistrationResponse.EnsureSuccessStatusCode();

        // Act: Try to register with same username
        var secondRegistrationResponse = await _client.PostAsJsonAsync("/api/account/register", registrationModel2);

        // Assert
        secondRegistrationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
