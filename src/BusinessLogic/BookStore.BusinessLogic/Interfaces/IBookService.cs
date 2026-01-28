using BookStore.Models;
using BookStore.Utilities;

namespace BookStore.BusinessLogic.Interfaces;

public interface IBookService
{
    Task<Result<BookDetailsDto>> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default);
    Task<Result<BookDetailsDto>> UpdateAsync(Guid id, UpdateBookRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<BookDetailsDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<BookDto>>> GetBooksAsync(BookFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<BookDetailsDto>> AssignAuthorToBookAsync(AssignAuthorToBookRequest request, CancellationToken cancellationToken = default);
    Task<Result<BookDetailsDto>> RemoveAuthorFromBookAsync(Guid bookId, Guid authorId, CancellationToken cancellationToken = default);
}

