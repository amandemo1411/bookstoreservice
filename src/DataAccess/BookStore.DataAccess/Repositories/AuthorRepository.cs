using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.Repositories;

public class AuthorRepository : GenericRepository<Author>, IAuthorRepository
{
    private readonly BookStoreDbContext _dbContext;

    public AuthorRepository(BookStoreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByNameAsync(string firstName, string lastName, CancellationToken cancellationToken = default)
        => _dbContext.Authors.AnyAsync(a => a.FirstName == firstName && a.LastName == lastName, cancellationToken);

    public async Task<IReadOnlyList<Author>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Authors
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Author>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idArray = ids.Distinct().ToArray();
        if (idArray.Length == 0)
        {
            return Array.Empty<Author>();
        }

        return await _dbContext.Authors
            .Where(a => idArray.Contains(a.Id))
            .ToListAsync(cancellationToken);
    }
}

