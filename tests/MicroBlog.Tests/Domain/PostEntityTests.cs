using System;
using Xunit;
using MicroBlog.Domain.Entities;
using FluentAssertions;

namespace MicroBlog.Tests.Domain;

public class PostEntityTests
{
    [Fact]
    public void CreatePost_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var userId = "user123";
        var text = "Test post content";

        // Act
        var post = new Post
        {
            UserId = userId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        post.UserId.Should().Be(userId);
        post.Text.Should().Be(text);
        post.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Post_WithImage_ShouldSetImageProperties()
    {
        // Arrange
        var imageUrl = "https://example.com/image.jpg";

        // Act
        var post = new Post
        {
            Text = "Test post with image",
            UserId = "user1",
            ImageUrl = imageUrl,
            ImageProcessingComplete = false
        };

        // Assert
        post.ImageUrl.Should().Be(imageUrl);
        post.ImageProcessingComplete.Should().BeFalse();
    }

    [Fact]
    public void Post_GeographicCoordinates_ShouldBeValid()
    {
        // Arrange & Act
        var post = new Post
        {
            Text = "Test geographic post",
            UserId = "user1",
            Latitude = 40.7128,
            Longitude = -74.0060
        };

        // Assert
        post.Latitude.Should().BeInRange(-90, 90);
        post.Longitude.Should().BeInRange(-180, 180);
    }

    [Fact]
    public void Post_TextLengthValidation_ShouldEnforceMaxLength()
    {
        // Arrange
        var validText = new string('x', 140);
        var invalidText = new string('x', 141);

        // Act & Assert
        var validPost = new Post { Text = validText, UserId = "user1" };  // Should not throw
        Assert.Throws<ArgumentException>(() => new Post { Text = invalidText, UserId = "user1" });
    }

    [Fact]
    public void Post_LikeAndCommentCounts_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var post = new Post
        {
            Text = "Test post with counts",
            UserId = "user1",
            LikeCount = 5,
            CommentCount = 3
        };

        // Assert
        post.LikeCount.Should().Be(5);
        post.CommentCount.Should().Be(3);
    }

    [Fact]
    public void Post_MultipleImageUrls_ShouldSetCorrectly()
    {
        // Arrange & Act
        var post = new Post
        {
            Text = "Test post with multiple images",
            UserId = "user1",
            OriginalImageUrl = "original.jpg",
            ImageUrl = "processed.jpg",
            WebPImageUrl = "optimized.webp",
            SmallImageUrl = "small.webp",
            MediumImageUrl = "medium.webp",
            LargeImageUrl = "large.webp"
        };

        // Assert
        post.OriginalImageUrl.Should().Be("original.jpg");
        post.ImageUrl.Should().Be("processed.jpg");
        post.WebPImageUrl.Should().Be("optimized.webp");
        post.SmallImageUrl.Should().Be("small.webp");
        post.MediumImageUrl.Should().Be("medium.webp");
        post.LargeImageUrl.Should().Be("large.webp");
    }

    [Fact]
    public void Post_RandomCoordinates_ShouldGenerateValidLocation()
    {
        // Arrange
        var random = new Random();

        // Act
        var post = new Post
        {
            Text = "Test random coordinates",
            UserId = "user1",
            Latitude = random.NextDouble() * 180 - 90,
            Longitude = random.NextDouble() * 360 - 180
        };

        // Assert
        post.Latitude.Should().BeInRange(-90, 90);
        post.Longitude.Should().BeInRange(-180, 180);
    }

    [Fact]
    public void Post_CreationTimestamp_ShouldBeUtc()
    {
        // Arrange & Act
        var post = new Post
        {
            Text = "Test UTC timestamp",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        post.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Post_UserProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var post = new Post
        {
            Text = "Test user post",
            UserId = "user123",
            UserName = "testuser"
        };

        // Assert
        post.UserId.Should().Be("user123");
        post.UserName.Should().Be("testuser");
    }

    [Fact]
    public void Post_ImageProcessingStatus_ShouldTrackCorrectly()
    {
        // Arrange & Act
        var post = new Post
        {
            Text = "Test post",
            UserId = "user1",
            ImageProcessingComplete = false
        };

        // Assert
        post.ImageProcessingComplete.Should().BeFalse();

        // Change status
        post.ImageProcessingComplete = true;
        post.ImageProcessingComplete.Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Valid post", true)]
    public void Post_TextValidation_ShouldCheckNullOrEmptyText(string text, bool isValid)
    {
        // Arrange & Act
        if (isValid)
        {
            var post = new Post { Text = text, UserId = "user1" };  // Should not throw
            post.Text.Should().Be(text);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => new Post { Text = text, UserId = "user1" });
        }
    }
}
