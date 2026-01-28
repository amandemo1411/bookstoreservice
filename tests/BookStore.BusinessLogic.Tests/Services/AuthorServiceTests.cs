using BookStore.BusinessLogic.Services;
using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BookStore.BusinessLogic.Tests.Services;

public class AuthorServiceTests
{
    private readonly Mock<IAuthorRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly AuthorService _service;

    public AuthorServiceTests()
    {
        _service = new AuthorService(
            _repo.Object,
            _uow.Object,
            Mock.Of<ILogger<AuthorService>>(),
            _cache);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenAuthorExists()
    {
        var request = new CreateAuthorRequest("John", "Doe");
        _repo.Setup(r => r.ExistsByNameAsync("John", "Doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Author with the same first and last name already exists.", result.Error);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryAndUow_OnSuccess()
    {
        var request = new CreateAuthorRequest("John", "Doe");
        _repo.Setup(r => r.ExistsByNameAsync("John", "Doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _uow
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> action, CancellationToken ct) => action(ct));

        var result = await _service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        _repo.Verify(r => r.AddAsync(It.IsAny<Author>()), Times.Once);
    }
}

