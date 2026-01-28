using BookStore.Models;
using BookStore.Utilities;

namespace BookStore.DataAccess.Interfaces;

public interface IBookRepository : IGenericRepository<Book>
{
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    Task<Book?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Book?> GetWithAuthorsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Book>> GetPagedAsync(BookFilterRequest filter, CancellationToken cancellationToken = default);
}

