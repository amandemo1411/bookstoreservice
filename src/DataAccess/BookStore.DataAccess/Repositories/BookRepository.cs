using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    private readonly BookStoreDbContext _dbContext;

    public BookRepository(BookStoreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        => _dbContext.Books.FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);

    public Task<Book?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Books
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .Include(b => b.StoreBooks)
                .ThenInclude(sb => sb.Store)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Book?> GetWithAuthorsAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Books
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<PagedResult<Book>> GetPagedAsync(BookFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Books
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            query = query.Where(b => EF.Functions.Like(b.Title, $"%{filter.Title}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.AuthorName))
        {
            var authorName = filter.AuthorName.Trim();
            query = query.Where(b =>
                b.BookAuthors.Any(ba =>
                    (ba.Author.FirstName + " " + ba.Author.LastName).Contains(authorName)));
        }

        var total = await query.CountAsync(cancellationToken);

        var sortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "Title" : filter.SortBy!;
        var desc = filter.Desc;

        query = sortBy.ToLowerInvariant() switch
        {
            "isbn" => desc ? query.OrderByDescending(b => b.Isbn) : query.OrderBy(b => b.Isbn),
            "createdat" => desc ? query.OrderByDescending(b => b.CreatedAtUtc) : query.OrderBy(b => b.CreatedAtUtc),
            _ => desc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
        };

        var pagination = new Pagination(filter.Page, filter.PageSize, filter.SortBy, filter.Desc);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Book>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            Total = total
        };
    }
}

