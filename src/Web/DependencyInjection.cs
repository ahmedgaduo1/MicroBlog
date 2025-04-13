using Azure.Identity;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;

namespace MicroBlog.Web;

public static class DependencyInjection
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddRazorPages();
        services.AddHttpContextAccessor();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IUserService, UserService>();
        // Register both ImageProcessingService implementations
        services.AddScoped<Web.Services.ImageProcessingService>();
        services.AddScoped<MicroBlog.Web.Services.IImageProcessingService>(sp => sp.GetRequiredService<Web.Services.ImageProcessingService>());
        services.AddScoped<MicroBlog.Application.Common.Interfaces.IImageProcessingService>(sp => sp.GetRequiredService<Web.Services.ImageProcessingService>());
        services.AddScoped<IUser, CurrentUser>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "MicroBlog API", Version = "v1" });
        });
        services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);
    }

    public static void AddKeyVaultIfConfigured(this IServiceCollection services, IConfiguration configuration)
    {
        var keyVaultUri = configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            // Azure Key Vault integration - using a simplified approach for this sample
            // In a real application, you'd use Azure.Extensions.AspNetCore.Configuration.Secrets
            // services.AddAzureKeyVault(...) would be the proper method
            services.Configure<AzureKeyVaultOptions>(options => 
            {
                options.VaultUri = new Uri(keyVaultUri);
                options.Credential = new DefaultAzureCredential();
            });
        }
    }
    
    // Placeholder class for Azure Key Vault options
    public class AzureKeyVaultOptions
    {
        public Uri? VaultUri { get; set; }
        public DefaultAzureCredential? Credential { get; set; }
    }
}
