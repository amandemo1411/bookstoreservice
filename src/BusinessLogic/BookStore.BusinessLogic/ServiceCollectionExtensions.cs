using BookStore.BusinessLogic.Interfaces;
using BookStore.BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.BusinessLogic;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookStoreServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<ISeedService, SeedService>();

        return services;
    }
}

