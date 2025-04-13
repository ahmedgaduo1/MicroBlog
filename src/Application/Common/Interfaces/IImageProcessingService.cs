using Microsoft.AspNetCore.Http;

namespace MicroBlog.Application.Common.Interfaces;

public interface IImageProcessingService
{
    Task<string> SaveOriginalImageAsync(IFormFile file);
    Task ProcessImageAsync(int postId, string originalImageUrl);
    bool IsValidImage(IFormFile file);
    Task<(int width, int height)> GetImageDimensionsAsync(IFormFile file);
}
