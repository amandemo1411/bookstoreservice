using System.Text.Json;
using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.Repositories;

public class SeedRepository : ISeedRepository
{
    private readonly BookStoreDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public SeedRepository(BookStoreDbContext dbContext, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Books.AnyAsync(cancellationToken)
               || await _dbContext.Authors.AnyAsync(cancellationToken)
               || await _dbContext.Stores.AnyAsync(cancellationToken);
    }

    public async Task SeedFromFileAsync(string jsonFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException("Seed data file not found.", jsonFilePath);
        }

        await using var stream = File.OpenRead(jsonFilePath);
        var seedData = await JsonSerializer.DeserializeAsync<SeedData>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize seed data.");

        // Take defensive copies of collections to avoid 'collection modified' issues
        var authorsSeed = seedData.Authors.ToList();
        var storesSeed = seedData.Stores.ToList();
        var booksSeed = seedData.Books.ToList();
        var bookAuthorsSeed = seedData.BookAuthors.ToList();
        var storeBooksSeed = seedData.StoreBooks.ToList();

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Create authors
            var authorMap = new Dictionary<string, Author>();
            foreach (var a in authorsSeed)
            {
                var author = new Author { FirstName = a.FirstName, LastName = a.LastName };
                authorMap[a.Id] = author;
                await _dbContext.Authors.AddAsync(author, ct);
            }

            // Create stores
            var storeMap = new Dictionary<string, Store>();
            foreach (var s in storesSeed)
            {
                var store = new Store { Name = s.Name, Location = s.Location };
                storeMap[s.Id] = store;
                await _dbContext.Stores.AddAsync(store, ct);
            }

            // Create books
            var bookMap = new Dictionary<string, Book>();
            foreach (var b in booksSeed)
            {
                var book = new Book { Isbn = b.Isbn, Title = b.Title, Description = b.Description };
                bookMap[b.Id] = book;
                await _dbContext.Books.AddAsync(book, ct);
            }

            await _dbContext.SaveChangesAsync(ct);

            // Create book-author links
            foreach (var ba in bookAuthorsSeed)
            {
                if (!bookMap.TryGetValue(ba.BookId, out var book) ||
                    !authorMap.TryGetValue(ba.AuthorId, out var author))
                {
                    continue;
                }

                _dbContext.BookAuthors.Add(new BookAuthor
                {
                    BookId = book.Id,
                    AuthorId = author.Id
                });
            }

            // Create store-book links
            foreach (var sb in storeBooksSeed)
            {
                if (!storeMap.TryGetValue(sb.StoreId, out var store) ||
                    !bookMap.TryGetValue(sb.BookId, out var book))
                {
                    continue;
                }

                _dbContext.StoreBooks.Add(new StoreBook
                {
                    StoreId = store.Id,
                    BookId = book.Id,
                    Quantity = sb.Quantity
                });
            }

            await _dbContext.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    private sealed record SeedAuthor(string Id, string FirstName, string LastName);
    private sealed record SeedStore(string Id, string Name, string? Location);
    private sealed record SeedBook(string Id, string Isbn, string Title, string? Description);
    private sealed record SeedBookAuthor(string BookId, string AuthorId);
    private sealed record SeedStoreBook(string StoreId, string BookId, int Quantity);

    private sealed record SeedData(
        IReadOnlyCollection<SeedAuthor> Authors,
        IReadOnlyCollection<SeedStore> Stores,
        IReadOnlyCollection<SeedBook> Books,
        IReadOnlyCollection<SeedBookAuthor> BookAuthors,
        IReadOnlyCollection<SeedStoreBook> StoreBooks);
}

