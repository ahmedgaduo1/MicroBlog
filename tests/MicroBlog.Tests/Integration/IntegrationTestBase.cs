using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Web;

namespace MicroBlog.Tests.Integration;

public abstract class IntegrationTestBase : IDisposable
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;
    protected readonly IServiceScope _scope;
    protected readonly ApplicationDbContext _dbContext;

    protected IntegrationTestBase()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DB context registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Use SQL Server with a unique test database name for each test run
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MicroBlogTest_" + Guid.NewGuid().ToString("N") + ";Trusted_Connection=True;MultipleActiveResultSets=true");
                    });

                    // Ensure database is created and seeded
                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                        
                        // Ensure database is created
                        db.Database.EnsureCreated();
                    }
                });
            });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _scope.Dispose();
        _dbContext.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Helper method to create an authenticated test user
    /// </summary>
    protected async Task<string> CreateTestUserAndGetTokenAsync()
    {
        var registrationModel = new 
        {
            UserName = $"testuser_{Guid.NewGuid():N}",
            Email = $"testuser_{Guid.NewGuid():N}@example.com",
            Password = "Test@Password123!"
        };

        var registrationResponse = await _client.PostAsJsonAsync("/api/account/register", registrationModel);
        registrationResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new 
        {
            UserName = registrationModel.UserName,
            Password = registrationModel.Password
        });
        
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        
        return loginResult.Token;
    }

    // Helper class for login response
    private class LoginResponse
    {
        public string Token { get; set; }
    }
}
