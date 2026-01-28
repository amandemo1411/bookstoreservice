using BookStore.API.Controllers;
using BookStore.BusinessLogic.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BookStore.API.Tests.Controllers;

public class AuthorsControllerTests
{
    private readonly Mock<IAuthorService> _authorServiceMock = new();
    private readonly AuthorsController _controller;

    public AuthorsControllerTests()
    {
        _controller = new AuthorsController(_authorServiceMock.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<AuthorsController>>());
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenServiceSucceeds()
    {
        // Arrange
        var request = new CreateAuthorRequest("John", "Doe");
        var created = new AuthorDto(Guid.NewGuid(), request.FirstName, request.LastName);
        _authorServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthorDto>.Ok(created));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<AuthorDto>(createdAt.Value);
        Assert.Equal(created.Id, dto.Id);
    }
}

