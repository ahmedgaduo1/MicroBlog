using MediatR;
using Microsoft.AspNetCore.Http;

namespace MicroBlog.Application.Posts.Commands.CreatePost;

public record CreatePostCommand : IRequest<int>
{
    public required string Text { get; init; }
    
    public IFormFile? Image { get; init; }
}
