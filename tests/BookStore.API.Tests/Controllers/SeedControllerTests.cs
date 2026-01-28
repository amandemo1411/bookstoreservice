using BookStore.API.Controllers;
using BookStore.BusinessLogic.Interfaces;
using BookStore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BookStore.API.Tests.Controllers;

public class SeedControllerTests
{
    private readonly Mock<ISeedService> _seedServiceMock = new();
    private readonly SeedController _controller;

    public SeedControllerTests()
    {
        _controller = new SeedController(_seedServiceMock.Object);
    }

    [Fact]
    public async Task Seed_ReturnsOk_WhenServiceSucceeds()
    {
        _seedServiceMock
            .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("Database seeded."));

        var result = await _controller.Seed(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Database seeded.", ok.Value);
    }
}

