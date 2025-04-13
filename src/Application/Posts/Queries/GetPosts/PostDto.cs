using MicroBlog.Domain.Entities;

namespace MicroBlog.Application.Posts.Queries.GetPosts;

public class PostDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool ImageProcessingComplete { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTimeOffset Created { get; set; }
    public List<PostImageDto> Images { get; set; } = new();
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Post, PostDto>();
            CreateMap<PostImage, PostImageDto>();
        }
    }
}
