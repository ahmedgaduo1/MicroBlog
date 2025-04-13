/**
 * Exception thrown when there's an error with blob storage operations.
 */

namespace MicroBlog.Infrastructure.Common.Exceptions;
public class BlobStorageException : Exception
{
    public BlobStorageException(string message, Exception innerException) 
        : base(message, innerException) { }

    public BlobStorageException(string message) 
        : base(message) { }
}
