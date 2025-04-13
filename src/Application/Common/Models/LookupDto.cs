using MicroBlog.Domain.Entities;

namespace MicroBlog.Application.Common.Models;

public class LookupDto
{
    public int Id { get; init; }

    public string? Title { get; init; }
}
