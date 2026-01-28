using BookStore.BusinessLogic.Services;
using BookStore.DataAccess.Interfaces;
using BookStore.Utilities;
using Moq;
using Xunit;

namespace BookStore.BusinessLogic.Tests.Services;

public class SeedServiceTests
{
    private readonly Mock<ISeedRepository> _seedRepo = new();
    private readonly SeedService _service;

    public SeedServiceTests()
    {
        _service = new SeedService(_seedRepo.Object);
    }

    [Fact]
    public async Task SeedAsync_ReturnsAlreadySeeded_WhenIsSeeded()
    {
        _seedRepo.Setup(r => r.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.SeedAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Database already seeded.", result.Value);
    }

    [Fact]
    public async Task SeedAsync_DelegatesToRepository_WhenNotSeeded()
    {
        _seedRepo.Setup(r => r.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _seedRepo.Setup(r => r.SeedFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.SeedAsync(CancellationToken.None);

        Assert.True(result.Success);
        _seedRepo.Verify(r => r.SeedFromFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

