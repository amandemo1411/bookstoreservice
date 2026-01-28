using BookStore.DataAccess;
using BookStore.DataAccess.Repositories;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookStore.DataAccess.Tests.Repositories;

public class BookRepositoryTests
{
    private readonly BookStoreDbContext _dbContext;
    private readonly BookRepository _repository;

    public BookRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new BookStoreDbContext(options);
        _repository = new BookRepository(_dbContext);
    }

    [Fact]
    public async Task GetByIsbnAsync_ReturnsBook_WhenExists()
    {
        var book = new Book { Isbn = "123", Title = "Test", Description = null };
        await _dbContext.Books.AddAsync(book);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetByIsbnAsync("123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("123", result!.Isbn);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitle()
    {
        await _dbContext.Books.AddRangeAsync(
            new Book { Isbn = "1", Title = "Test Book", Description = null },
            new Book { Isbn = "2", Title = "Other", Description = null });
        await _dbContext.SaveChangesAsync();

        var filter = new BookFilterRequest("Test", null, 1, 10, null, false);

        var paged = await _repository.GetPagedAsync(filter, CancellationToken.None);

        Assert.Single(paged.Items);
        Assert.Equal("Test Book", paged.Items.First().Title);
    }
}

