using Microsoft.Extensions.Logging;
using MicroBlog.Infrastructure.Common.Exceptions;
using MicroBlog.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

/**
 * Service for handling local file storage as a fallback.
 * Stores images in a local directory structure.
 */

namespace MicroBlog.Infrastructure.Common.LocalStorage;
public class LocalStorageService: ILocalStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _baseUrl;

    public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
    {
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), 
            configuration["LocalStorage:Path"] ?? "local-storage");
        _baseUrl = configuration["LocalStorage:BaseUrl"] ?? "/storage";
        _logger = logger;
        EnsureStorageDirectoryExists();
    }

    /**
     * Ensures the storage directory exists.
     */
    private void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    /**
     * Saves a file locally.
     * 
     * @param fileBytes The bytes containing the file data
     * @param fileName The name of the file
     * @param contentType The content type of the file
     * @returns The URL path to the stored file
     */
    public async Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string contentType)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, fileName);
            
            await File.WriteAllBytesAsync(filePath, fileBytes);
            
            return await GetFileUrlAsync(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Failed to store file locally: {FileName}", fileName);
            var errorMessage = $"Failed to store file locally: {fileName}";
            throw new LocalStorageException(errorMessage, ex);
        }
    }

    /**
     * Deletes a locally stored file.
     * 
     * @param fileName The name of the file to delete
     * @returns True if successful, false otherwise
     */
    public Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Failed to delete local file: {FileName}", fileName);
            var errorMessage = $"Failed to delete local file: {fileName}";
            throw new LocalStorageException(errorMessage, ex);
        }
    }

    /**
     * Gets the URL for a locally stored file.
     * 
     * @param fileName The name of the file
     * @returns The URL to access the file
     */
    public Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            // Combine with base URL to create a web-accessible path
            string url = $"{_baseUrl}/{fileName}";
            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Failed to get URL for local file: {FileName}", fileName);
            var errorMessage = $"Failed to get URL for local file: {fileName}";
            throw new LocalStorageException(errorMessage, ex);
        }
    }
}
