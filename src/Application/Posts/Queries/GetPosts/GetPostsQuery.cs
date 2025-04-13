using MediatR;

namespace MicroBlog.Application.Posts.Queries.GetPosts;

public record GetPostsQuery : IRequest<PostsVm>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
