using BookStore.API.Controllers;
using BookStore.BusinessLogic.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BookStore.API.Tests.Controllers;

public class BooksControllerTests
{
    private readonly Mock<IBookService> _bookServiceMock = new();
    private readonly BooksController _controller;

    public BooksControllerTests()
    {
        _controller = new BooksController(_bookServiceMock.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<BooksController>>());
    }

    [Fact]
    public async Task GetBooks_ReturnsOk_WithPagedResult()
    {
        // Arrange
        var filter = new BookFilterRequest("1984", null, 1, 10, null, false);
        var paged = new PagedResult<BookDto>
        {
            Items = new[] { new BookDto(Guid.NewGuid(), "isbn", "1984", null) },
            Page = 1,
            PageSize = 10,
            Total = 1
        };

        _bookServiceMock
            .Setup(s => s.GetBooksAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BookDto>>.Ok(paged));

        // Act
        var result = await _controller.GetBooks(filter, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<PagedResult<BookDto>>(ok.Value);
        Assert.Single(value.Items);
    }
}

