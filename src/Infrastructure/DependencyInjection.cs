using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Constants;
using MicroBlog.Domain.Identity;
using MicroBlog.Infrastructure.BlobStorage;
using MicroBlog.Infrastructure.Common.LocalStorage;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Infrastructure.Data.Interceptors;
using MicroBlog.Infrastructure.Identity;
using MicroBlog.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MicroBlogDb");
        Guard.Against.NullOrEmpty(connectionString, "Connection string 'MicroBlogDb' not found.");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        services
            .AddDefaultIdentity<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));

        // Register blob storage service
        services.AddTransient<IBlobStorageService, AzureBlobStorageService>();

        // Register local storage service as fallback
        services.AddTransient<ILocalStorageService, MicroBlog.Infrastructure.Common.LocalStorage.LocalStorageService>();
    }
}
