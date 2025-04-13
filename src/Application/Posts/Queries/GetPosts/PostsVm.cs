using MicroBlog.Application.Posts.Queries.GetPosts;

namespace MicroBlog.Application.Posts.Queries.GetPosts;

public class PostsVm
{
    public List<PostDto> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
