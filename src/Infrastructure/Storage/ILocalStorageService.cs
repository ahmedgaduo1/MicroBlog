using System.Threading.Tasks;

namespace MicroBlog.Infrastructure.Storage;

public interface ILocalStorageService
{
    Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string fileName);
    Task<string> GetFileUrlAsync(string fileName);
}
