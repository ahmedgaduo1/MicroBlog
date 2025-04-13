using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Identity;
using MicroBlog.Infrastructure.BlobStorage;
using MicroBlog.Infrastructure.Common.LocalStorage;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Infrastructure.Data.Interceptors;
using MicroBlog.Infrastructure.Identity;
using MicroBlog.Infrastructure.Services;
using MicroBlog.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroBlog.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MicroBlogDb");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=.;Database=MicroBlogDb;Trusted_Connection=True;MultipleActiveResultSets=true";
        }

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

        // Configure JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddTransient<IIdentityService, IdentityService>();

        // Register services
        services.AddTransient<IBlobStorageService, AzureBlobStorageService>();
        services.AddTransient<ILocalStorageService, MicroBlog.Infrastructure.Common.LocalStorage.LocalStorageService>();
        
        return services;
    }
}
