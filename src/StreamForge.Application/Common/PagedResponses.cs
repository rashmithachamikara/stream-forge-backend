using StreamForge.Application.DTOs.Content;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Common;

/// <summary>
/// Provides helper methods for normalizing paging inputs and constructing paged API responses.
/// </summary>
public static class PagedResponses
{
    /// <summary>
    /// Normalizes a requested page number to a valid positive value.
    /// </summary>
    /// <param name="page">Requested page number.</param>
    /// <param name="defaultPage">Fallback page number used when the requested page is invalid.</param>
    /// <returns>A normalized page number.</returns>
    public static int NormalizePage(int page, int defaultPage = 1) => page <= 0 ? defaultPage : page;

    /// <summary>
    /// Normalizes a requested page size to a valid bounded value.
    /// </summary>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="defaultPageSize">Fallback page size used when the requested size is invalid.</param>
    /// <param name="maxPageSize">Maximum allowed page size.</param>
    /// <returns>A normalized page size.</returns>
    public static int NormalizePageSize(int pageSize, int defaultPageSize = 25, int maxPageSize = 100)
    {
        if (pageSize <= 0)
        {
            return defaultPageSize;
        }

        return Math.Min(pageSize, maxPageSize);
    }

    /// <summary>
    /// Maps a paged query result into a paged response DTO.
    /// </summary>
    /// <typeparam name="TSource">Source item type.</typeparam>
    /// <typeparam name="TDestination">Destination item type.</typeparam>
    /// <param name="result">Paged query result.</param>
    /// <param name="map">Item mapping function.</param>
    /// <returns>A paged response DTO with mapped items and paging metadata.</returns>
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

    /// <summary>
    /// Creates a paged response DTO from already materialized items and paging metadata.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="items">Page items.</param>
    /// <param name="page">Current page number.</param>
    /// <param name="pageSize">Current page size.</param>
    /// <param name="totalCount">Total number of items across all pages.</param>
    /// <returns>A paged response DTO with calculated page metadata.</returns>
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
