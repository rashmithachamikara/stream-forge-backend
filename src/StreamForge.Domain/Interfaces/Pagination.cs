namespace StreamForge.Domain.Interfaces;

public sealed record PagedQueryResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
