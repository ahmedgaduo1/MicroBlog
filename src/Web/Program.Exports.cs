using Microsoft.AspNetCore.Builder;

namespace MicroBlog.Web;

/// <summary>
/// This public class exposes our Program entry point to test projects for integration testing
/// </summary>
public class Program
{
    // This method would not be used, but it's needed to expose the type
    protected Program() { }

    // Keep the WebApplication builder method public for test projects to access
    public static WebApplicationBuilder CreateHostBuilder(string[] args) => WebApplication.CreateBuilder(args);
}
