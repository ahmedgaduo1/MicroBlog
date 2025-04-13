namespace MicroBlog.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when operations with blob storage fail
/// </summary>
public class BlobStorageException : Exception
{
    public BlobStorageException()
        : base("An error occurred with the blob storage.")
    {
    }

    public BlobStorageException(string message)
        : base(message)
    {
    }

    public BlobStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
