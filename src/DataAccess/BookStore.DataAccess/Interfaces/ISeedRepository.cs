namespace BookStore.DataAccess.Interfaces;

public interface ISeedRepository
{
    Task<bool> IsSeededAsync(CancellationToken cancellationToken = default);
    Task SeedFromFileAsync(string jsonFilePath, CancellationToken cancellationToken = default);
}

