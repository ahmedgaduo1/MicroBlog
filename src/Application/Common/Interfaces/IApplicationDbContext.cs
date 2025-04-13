using MicroBlog.Domain.Entities;
using MicroBlog.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroBlog.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    
    DbSet<Post> Posts { get; }

    DbSet<PostImage> PostImages { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
