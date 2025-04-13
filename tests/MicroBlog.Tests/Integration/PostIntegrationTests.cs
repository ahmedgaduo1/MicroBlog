using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MicroBlog.Domain.Entities;
using MicroBlog.Infrastructure.Data;
using MicroBlog.Tests.Models;
using MicroBlog.Web.Models;

namespace MicroBlog.Tests.Integration;

public class PostIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreatePost_WithValidData_ShouldSucceed()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var postModel = new PostCreateModel
        {
            Text = "Integration test post content",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", postModel);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdPost = await response.Content.ReadFromJsonAsync<PostViewModel>();
        
        createdPost.Should().NotBeNull();
        createdPost.Text.Should().Be(postModel.Text);
    }

    [Fact]
    public async Task CreatePost_WithLongText_ShouldFail()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var postModel = new PostCreateModel
        {
            Text = new string('x', 141)  // Exceeds 140 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", postModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPostTimeline_ShouldReturnPosts()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        // Create multiple posts
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/posts", new PostCreateModel
            {
                Text = $"Timeline test post {i}"
            });
        }

        // Act
        var timelineResponse = await _client.GetAsync("/api/posts/timeline");

        // Assert
        timelineResponse.EnsureSuccessStatusCode();
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<List<PostViewModel>>();
        
        timeline.Should().NotBeNull();
        timeline.Count.Should().Be(5);
        timeline.Should().BeInDescendingOrder(p => p.PostedAt);
    }

    [Fact]
    public async Task CreatePost_WithImage_ShouldProcessImage()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        // Create multipart content with image
        using var content = new MultipartFormDataContent();
        
        // Add text content
        content.Add(new StringContent("Post with image"), "Text");

        // Add image file
        var imageContent = new ByteArrayContent(File.ReadAllBytes("./TestData/test-image.jpg"));
        imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "Image", "test-image.jpg");

        // Act
        var response = await _client.PostAsync("/api/posts", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdPost = await response.Content.ReadFromJsonAsync<PostViewModel>();
        
        createdPost.Should().NotBeNull();
        createdPost.ImageUrl.Should().NotBeNullOrEmpty();
        createdPost.ImageProcessingComplete.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePost_Unauthorized_ShouldFail()
    {
        // Arrange
        // Clear any existing authorization
        _client.DefaultRequestHeaders.Authorization = null;

        var postModel = new PostCreateModel
        {
            Text = "Unauthorized post attempt"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", postModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
