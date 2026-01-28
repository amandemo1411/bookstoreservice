using BookStore.Models;

namespace BookStore.DataAccess.Interfaces;

public interface IStoreRepository : IGenericRepository<Store>
{
    Task<IReadOnlyList<Store>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Store?> GetWithBooksAsync(Guid id, bool trackChanges, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Store>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}

