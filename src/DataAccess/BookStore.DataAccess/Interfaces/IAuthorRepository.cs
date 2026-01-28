using BookStore.Models;

namespace BookStore.DataAccess.Interfaces;

public interface IAuthorRepository : IGenericRepository<Author>
{
    Task<bool> ExistsByNameAsync(string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Author>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Author>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}

