using BookStore.BusinessLogic.Interfaces;
using BookStore.DataAccess.Interfaces;
using BookStore.Utilities;

namespace BookStore.BusinessLogic.Services;

public class SeedService : ISeedService
{
    private readonly ISeedRepository _seedRepository;

    public SeedService(ISeedRepository seedRepository)
    {
        _seedRepository = seedRepository;
    }

    public async Task<Result<string>> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _seedRepository.IsSeededAsync(cancellationToken))
        {
            return Result<string>.Ok("Database already seeded.");
        }

        var baseDir = AppContext.BaseDirectory;
        var jsonPath = Path.Combine(baseDir, "Seed", "seed-data.json");

        try
        {
            await _seedRepository.SeedFromFileAsync(jsonPath, cancellationToken);
            return Result<string>.Ok("Database seeded.");
        }
        catch (FileNotFoundException)
        {
            return Result<string>.Fail($"Seed data file not found at path '{jsonPath}'.");
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to seed database: {ex.Message}");
        }
    }
}

