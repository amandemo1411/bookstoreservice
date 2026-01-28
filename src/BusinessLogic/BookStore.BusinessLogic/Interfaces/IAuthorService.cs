using BookStore.Models;
using BookStore.Utilities;

namespace BookStore.BusinessLogic.Interfaces;

public interface IAuthorService
{
    Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<AuthorDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AuthorDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

