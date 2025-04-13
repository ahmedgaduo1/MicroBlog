/**
 * Exception thrown when there's an error with local storage operations.
 */
namespace MicroBlog.Infrastructure.Common.Exceptions;
public class LocalStorageException : Exception
{
    public LocalStorageException(string message, Exception innerException) 
        : base(message, innerException) { }

    public LocalStorageException(string message) 
        : base(message) { }
}
