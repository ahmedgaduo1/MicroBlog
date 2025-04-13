using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MicroBlog.Web.Controllers;
using MicroBlog.Web.Services;
using MicroBlog.Web.Models;
using FluentAssertions;

namespace MicroBlog.Tests.Controllers;

public class CommentApiControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<ILogger<CommentApiController>> _mockLogger;
    private readonly CommentApiController _controller;

    public CommentApiControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockLogger = new Mock<ILogger<CommentApiController>>();

        _controller = new CommentApiController(_mockPostService.Object, _mockLogger.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task AddComment_ValidInput_ShouldReturnOkResult()
    {
        // Arrange
        var commentModel = new CommentCreateModel 
        { 
            PostId = 1, 
            Text = "Test comment" 
        };

        _mockPostService
            .Setup(s => s.AddCommentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new CommentViewModel());

        // Act
        var result = await _controller.AddComment(commentModel);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AddComment_InvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Text", "Required");
        var commentModel = new CommentCreateModel();

        // Act
        var result = await _controller.AddComment(commentModel);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetComments_ValidPostId_ShouldReturnComments()
    {
        // Arrange
        int postId = 1;
        _mockPostService
            .Setup(s => s.GetCommentsForPostAsync(postId))
            .ReturnsAsync(new List<CommentViewModel>());

        // Act
        var result = await _controller.GetComments(postId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult.Value as dynamic;
        ((bool)response.success).Should().BeTrue();
    }
}
