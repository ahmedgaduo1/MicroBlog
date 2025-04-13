using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Application.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MicroBlog.Infrastructure.BlobStorage;

/**
 * Service for handling Azure Blob Storage operations.
 * Provides methods for uploading, downloading, and managing blobs.
 */
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string? _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Azure Blob Storage connection string not configured. Using local fallback storage.");
            // Will use local fallback in methods - _blobServiceClient remains null
        }
        else
        {
            _logger.LogInformation("Azure Blob Storage connection string found. Using Azure Blob Storage.");
            _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "microblog-images";
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
    }

    /**
     * Uploads a file to blob storage.
     * 
     * @param fileStream The stream containing the file data
     * @param fileName The name of the file
     * @param contentType The content type of the file
     * @returns The name of the uploaded blob
     * @throws BlobStorageException If there's an error during upload
     */
    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        // Generate a unique name for the blob
        var blobName = $"{Guid.NewGuid()}_{fileName}";
        
        // If Azure Blob Storage is not configured, use local fallback
        if (_blobServiceClient == null)
        {
            _logger.LogInformation("Using local storage fallback for file {FileName}", fileName);
            return await SaveToLocalStorageAsync(fileStream, blobName, contentType);
        }

        try
        {
            var container = await GetContainerAsync();
            var blob = container.GetBlobClient(blobName);

            var blobHttpHeader = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blob.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            _logger.LogInformation("Successfully uploaded blob {BlobName} to Azure storage", blobName);
            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {FileName} to Azure. Falling back to local storage.", fileName);
            return await SaveToLocalStorageAsync(fileStream, blobName, contentType);
        }
    }

    // Helper method for saving to local storage as a fallback
    private async Task<string> SaveToLocalStorageAsync(Stream fileStream, string blobName, string contentType)
    {
        try
        {
            // Create a local directory for storing blobs if it doesn't exist
            var localStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local-blobs");
            Directory.CreateDirectory(localStoragePath);
            
            // Save the file locally
            var filePath = Path.Combine(localStoragePath, blobName);
            using (var fileStream2 = new FileStream(filePath, FileMode.Create))
            {
                fileStream.Position = 0; // Reset position to beginning of stream
                await fileStream.CopyToAsync(fileStream2);
            }
            
            _logger.LogInformation("Successfully saved blob {BlobName} to local storage", blobName);
            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save blob {BlobName} to local storage", blobName);
            throw new BlobStorageException($"Failed to save image {blobName} to local storage", ex);
        }
    }

    /**
     * Deletes a blob from storage.
     * @param blobName The name of the blob to delete
     * @returns True if the blob was successfully deleted, false otherwise
     */
    public async Task<bool> DeleteAsync(string blobName)
    {
        // If Azure Blob Storage is not configured, use local fallback
        if (_blobServiceClient == null)
        {
            _logger.LogInformation("Using local storage fallback for deleting blob {BlobName}", blobName);
            return await DeleteFromLocalStorageAsync(blobName);
        }

        try
        {
            var container = await GetContainerAsync();
            var blob = container.GetBlobClient(blobName);
            var result = await blob.DeleteIfExistsAsync();
            
            _logger.LogInformation("Successfully deleted blob {BlobName} from Azure storage. Result: {Result}", blobName, result.Value);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobName} from Azure. Falling back to local storage.", blobName);
            return await DeleteFromLocalStorageAsync(blobName);
        }
    }

    // Helper method for deleting from local storage as a fallback
    private Task<bool> DeleteFromLocalStorageAsync(string blobName)
    {
        try
        {
            var localStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local-blobs");
            var filePath = Path.Combine(localStoragePath, blobName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Successfully deleted blob {BlobName} from local storage", blobName);
                return Task.FromResult(true);
            }
            
            _logger.LogWarning("Blob {BlobName} not found in local storage", blobName);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobName} from local storage", blobName);
            return Task.FromResult(false);
        }
    }

    // DeleteAsync method was implemented above

    /**
     * Gets the URL for a blob.
     * 
     * @param blobName The name of the blob
     * @returns The URL of the blob
     */
    public async Task<string> GetBlobUrlAsync(string blobName)
    {
        // If Azure Blob Storage is not configured, use local fallback
        if (_blobServiceClient == null)
        {
            _logger.LogInformation("Using local storage fallback for getting URL of blob {BlobName}", blobName);
            return GetLocalBlobUrl(blobName);
        }

        try
        {
            var container = await GetContainerAsync();
            var blob = container.GetBlobClient(blobName);
            return blob.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get URL for blob {BlobName} from Azure. Falling back to local storage.", blobName);
            return GetLocalBlobUrl(blobName);
        }
    }

    // Helper method for getting local blob URL
    private string GetLocalBlobUrl(string blobName)
    {
        // For local development, return a relative URL to the local-blobs directory
        return $"/local-blobs/{blobName}";
    }

    /**
     * Downloads a blob from storage.
     * 
     * @param blobName The name of the blob to download
     * @returns The stream containing the blob data
     */
    public async Task<Stream> DownloadAsync(string blobName)
    {
        // If Azure Blob Storage is not configured, use local fallback
        if (_blobServiceClient == null)
        {
            _logger.LogInformation("Using local storage fallback for downloading blob {BlobName}", blobName);
            return await DownloadFromLocalStorageAsync(blobName);
        }

        try
        {
            var container = await GetContainerAsync();
            var blob = container.GetBlobClient(blobName);
            var response = await blob.DownloadAsync();
            var memStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memStream);
            memStream.Position = 0;
            return memStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobName} from Azure. Falling back to local storage.", blobName);
            return await DownloadFromLocalStorageAsync(blobName);
        }
    }

    // Helper method for downloading from local storage
    private async Task<Stream> DownloadFromLocalStorageAsync(string blobName)
    {
        try
        {
            var localStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local-blobs");
            var filePath = Path.Combine(localStoragePath, blobName);
            
            if (File.Exists(filePath))
            {
                var memStream = new MemoryStream();
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await fileStream.CopyToAsync(memStream);
                }
                
                memStream.Position = 0;
                _logger.LogInformation("Successfully downloaded blob {BlobName} from local storage", blobName);
                return memStream;
            }
            
            _logger.LogWarning("Blob {BlobName} not found in local storage", blobName);
            throw new BlobStorageException($"Failed to find image {blobName} in local storage");
        }
        catch (Exception ex) when (!(ex is BlobStorageException))
        {
            _logger.LogError(ex, "Failed to download blob {BlobName} from local storage", blobName);
            throw new BlobStorageException($"Failed to download image {blobName} from local storage", ex);
        }
    }

    /**
     * Gets or creates the blob container.
     * 
     * @returns The blob container client
     */
    private async Task<BlobContainerClient> GetContainerAsync()
    {
        try
        {
            if (_blobServiceClient == null || string.IsNullOrEmpty(_containerName))
            {
                throw new BlobStorageException("Blob storage client or container name is not configured");
            }
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return containerClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create container {ContainerName}", _containerName);
            throw new BlobStorageException($"Failed to get or create container {_containerName}", ex);
        }
    }
}
