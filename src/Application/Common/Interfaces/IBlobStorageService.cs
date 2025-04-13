namespace MicroBlog.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAsync(string blobName);
    Task<string> GetBlobUrlAsync(string blobName);
    Task<Stream> DownloadAsync(string blobName);
}
