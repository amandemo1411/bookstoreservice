namespace BookStore.Utilities;

public class Result<T>
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public T? Value { get; init; }

    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static Result<T> Fail(string error) => new() { Success = false, Error = error };
}

public class PagedResult<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}

public record Pagination(int Page, int PageSize, string? SortBy = null, bool Desc = false)
{
    public int Skip => (Page - 1) * PageSize;
}
