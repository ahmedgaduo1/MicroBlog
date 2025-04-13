namespace MicroBlog.Tests.Models;

/// <summary>
/// Model used for creating new posts in integration tests
/// </summary>
public class PostCreateModel
{
    /// <summary>
    /// Text content of the post
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional image file path for the post
    /// </summary>
    public string? ImagePath { get; set; }
}
