using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using MicroBlog.Application;
using MicroBlog.Infrastructure;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Web;
using Hangfire;
using Hangfire.SqlServer;
using MicroBlog.Web.Infrastructure;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services from all layers using the extension methods
// From Application layer - using the ServiceCollectionExtensions class
MicroBlog.Application.ServiceCollectionExtensions.AddApplicationServices(builder.Services);

// From Infrastructure layer - using the ServiceCollectionExtensions class
MicroBlog.Infrastructure.ServiceCollectionExtensions.AddInfrastructureServices(builder.Services, builder.Configuration);

// From Web layer
MicroBlog.Web.DependencyInjection.AddWebServices(builder.Services);

// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("MicroBlogDb")));

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

// Register BackgroundImageService
builder.Services.AddScoped<MicroBlog.Web.Services.IBackgroundImageService, MicroBlog.Web.Services.BackgroundImageService>();

// Add Azure Key Vault if configured
builder.Services.AddKeyVaultIfConfigured(builder.Configuration);

// Log successful service registration
Console.WriteLine("All services registered successfully");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Add Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroBlog API v1"));
    
    // Initialize the database in development using the extension method
    try
    {
        Console.WriteLine("Initializing database...");
        await app.InitializeDatabaseAsync();
        Console.WriteLine("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

Console.WriteLine("Starting MicroBlog application...");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Set up static file middleware for local blob storage
var localBlobDirectory = Path.Combine(Directory.GetCurrentDirectory(), "local-blobs");
if (!Directory.Exists(localBlobDirectory))
{
    Directory.CreateDirectory(localBlobDirectory);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(localBlobDirectory),
    RequestPath = "/local-blobs"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire dashboard
app.UseHangfireDashboard();

// Register Hangfire recurring jobs
HangfireJobs.RegisterJobs();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Simplified configuration - remove Swagger for now

// Run the application
Console.WriteLine("Application starting - navigate to https://localhost:5001");

app.Run();

