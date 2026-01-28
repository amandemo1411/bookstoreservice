using System.Linq.Expressions;
using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly BookStoreDbContext DbContext;

    public GenericRepository(BookStoreDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task AddAsync(T entity)
    {
        await DbContext.Set<T>().AddAsync(entity);
    }

    public Task DeleteAsync(T entity)
    {
        DbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbContext.Set<T>();
        if (includes?.Any() == true)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbContext.Set<T>();
        if (includes?.Any() == true)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public Task UpdateAsync(T entity)
    {
        DbContext.Set<T>().Update(entity);
        return Task.CompletedTask;
    }
}
