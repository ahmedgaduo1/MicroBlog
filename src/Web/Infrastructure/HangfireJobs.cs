using Hangfire;
using MicroBlog.Web.Services;

namespace MicroBlog.Web.Infrastructure;

/// <summary>
/// Contains configuration and job registration for Hangfire background tasks
/// </summary>
public static class HangfireJobs
{
    /// <summary>
    /// Registers all recurring Hangfire jobs
    /// </summary>
    public static void RegisterJobs()
    {
        // Process local images to blob storage every 5 minutes
        RecurringJob.AddOrUpdate<IBackgroundImageService>(
            "process-local-images-to-blob",
            service => service.ProcessLocalImagesToBlob(),
            "*/5 * * * *"); // Cron expression for every 5 minutes
    }
}
