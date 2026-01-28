using BookStore.Models;
using BookStore.Utilities;

namespace BookStore.BusinessLogic.Interfaces;

public interface IStoreService
{
    Task<Result<StoreDto>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<StoreDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<StoreDetailsDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<BookDto>>> GetBooksForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<Result<StoreDetailsDto>> AssignBookToStoreAsync(AssignBookToStoreRequest request, CancellationToken cancellationToken = default);
    Task<Result<StoreDetailsDto>> RemoveBookFromStoreAsync(Guid storeId, Guid bookId, CancellationToken cancellationToken = default);
}

