using StreamForge.Application.DTOs.Content;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Common;

public static class PagedResponses
{
    public static int NormalizePage(int page, int defaultPage = 1) => page <= 0 ? defaultPage : page;

    public static int NormalizePageSize(int pageSize, int defaultPageSize = 25, int maxPageSize = 100)
    {
        if (pageSize <= 0)
        {
            return defaultPageSize;
        }

        return Math.Min(pageSize, maxPageSize);
    }

    public static PagedResponseDto<TDestination> Map<TSource, TDestination>(
        PagedQueryResult<TSource> result,
        Func<TSource, TDestination> map)
    {
        var totalPages = result.PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)result.TotalCount / result.PageSize);

        return new PagedResponseDto<TDestination>(
            result.Items.Select(map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            totalPages,
            result.Page < totalPages,
            result.Page > 1 && totalPages > 0);
    }

    public static PagedResponseDto<T> Create<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        var totalPages = pageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResponseDto<T>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            page < totalPages,
            page > 1 && totalPages > 0);
    }
}
