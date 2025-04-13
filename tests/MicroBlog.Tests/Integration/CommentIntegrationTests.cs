using System.Net;
using System.Net.Http.Json;
using MicroBlog.Tests.Models;
using MicroBlog.Web.Models;

namespace MicroBlog.Tests.Integration;

public class CommentIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task AddComment_ToExistingPost_ShouldSucceed()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        // Create a post first
        var postResponse = await _client.PostAsJsonAsync("/api/posts", new PostCreateModel
        {
            Text = "Post for commenting"
        });
        postResponse.EnsureSuccessStatusCode();
        var createdPost = await postResponse.Content.ReadFromJsonAsync<PostViewModel>();

        // Prepare comment
        var commentModel = new CommentCreateModel
        {
            PostId = createdPost.Id,
            Text = "Test comment on the post"
        };

        // Act
        var commentResponse = await _client.PostAsJsonAsync("/api/comments", commentModel);

        // Assert
        commentResponse.EnsureSuccessStatusCode();
        var addedComment = await commentResponse.Content.ReadFromJsonAsync<CommentViewModel>();
        
        addedComment.Should().NotBeNull();
        addedComment.Text.Should().Be(commentModel.Text);
        addedComment.PostId.Should().Be(createdPost.Id);
    }

    [Fact]
    public async Task GetComments_ForExistingPost_ShouldReturnComments()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        // Create a post
        var postResponse = await _client.PostAsJsonAsync("/api/posts", new PostCreateModel
        {
            Text = "Post for comment retrieval"
        });
        postResponse.EnsureSuccessStatusCode();
        var createdPost = await postResponse.Content.ReadFromJsonAsync<PostViewModel>();

        // Add multiple comments
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/comments", new CommentCreateModel
            {
                PostId = createdPost.Id,
                Text = $"Test comment {i}"
            });
        }

        // Act
        var commentsResponse = await _client.GetAsync($"/api/comments/{createdPost.Id}");

        // Assert
        commentsResponse.EnsureSuccessStatusCode();
        var comments = await commentsResponse.Content.ReadFromJsonAsync<List<CommentViewModel>>();
        
        comments.Should().NotBeNull();
        comments.Count.Should().Be(5);
    }

    [Fact]
    public async Task AddComment_Unauthorized_ShouldFail()
    {
        // Arrange
        // Clear any existing authorization
        _client.DefaultRequestHeaders.Authorization = null;

        var commentModel = new CommentCreateModel
        {
            PostId = 1,
            Text = "Unauthorized comment attempt"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/comments", commentModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddComment_ToNonExistentPost_ShouldFail()
    {
        // Arrange
        var authToken = await CreateTestUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var commentModel = new CommentCreateModel
        {
            PostId = int.MaxValue,  // Ensure this post doesn't exist
            Text = "Comment on non-existent post"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/comments", commentModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
