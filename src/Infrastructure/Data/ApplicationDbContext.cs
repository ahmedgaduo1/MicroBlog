using System.Reflection;
using System.Reflection.Emit;
using MicroBlog.Application.Common.Interfaces;
using MicroBlog.Domain.Entities;
using MicroBlog.Domain.Identity;
using MicroBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MicroBlog.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // This exposes the Users DbSet from IdentityDbContext
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<PostImage> PostImages => Set<PostImage>();

    DbSet<Domain.Identity.ApplicationUser> IApplicationDbContext.Users =>  Set<Domain.Identity.ApplicationUser>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Configure the many-to-many relationships
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Following)
            .WithMany(u => u.Followers);
            
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.LikedPosts)
            .WithMany(p => p.LikedByUsers);

        // Configure one-to-many relationship between User and Posts
        // Many-to-many relationship between Posts and Users (Likes)
        builder.Entity<Post>()
            .HasMany(p => p.LikedByUsers)
            .WithMany(u => u.LikedPosts)
            .UsingEntity<Dictionary<string, object>>(
                "PostLike", // Join table name
                j => j
                    .HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<Post>()
                    .WithMany()
                    .HasForeignKey("PostId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("PostId", "UserId");
                    j.ToTable("PostLikes");
                });

        // One-to-many: ApplicationUser -> Posts (Creator relationship)
        builder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts) // Or use: .WithMany(u => u.Posts) if you add a Posts collection
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict); // or Cascade, depending on your design
    }
}
