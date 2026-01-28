using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.Repositories;

public class StoreRepository : GenericRepository<Store>, IStoreRepository
{
    private readonly BookStoreDbContext _dbContext;

    public StoreRepository(BookStoreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Store>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Stores
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<Store?> GetWithBooksAsync(Guid id, bool trackChanges, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Stores
            .Include(s => s.StoreBooks)
                .ThenInclude(sb => sb.Book)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idArray = ids.Distinct().ToArray();
        if (idArray.Length == 0)
        {
            return Array.Empty<Store>();
        }

        return await _dbContext.Stores
            .Where(s => idArray.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }
}

