using MicroBlog.Domain.Entities;

namespace MicroBlog.Application.Posts.Queries.GetPosts;

public class PostImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
}
