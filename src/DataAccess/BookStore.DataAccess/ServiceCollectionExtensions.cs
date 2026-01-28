using BookStore.DataAccess.Interfaces;
using BookStore.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ISeedRepository, SeedRepository>();

        return services;
    }
}

