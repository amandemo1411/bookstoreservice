using BookStore.Utilities;

namespace BookStore.BusinessLogic.Interfaces;

public interface ISeedService
{
    Task<Result<string>> SeedAsync(CancellationToken cancellationToken = default);
}

