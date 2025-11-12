using Microsoft.Extensions.Caching.Memory;
using SS14.Admin.Models;

namespace SS14.Admin.Services;

/// <summary>
/// Implementation of IFilterKeyService using in-memory cache.
/// Filter keys expire after 30 minutes of inactivity (sliding expiration).
/// </summary>
public class FilterKeyService : IFilterKeyService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FilterKeyService> _logger;
    private const string CacheKeyPrefix = "FilterKey_";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    public FilterKeyService(IMemoryCache cache, ILogger<FilterKeyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string CreateFilterKey(FilterCriteria criteria)
    {
        // Generate cryptographically secure GUID
        var filterKey = Guid.NewGuid().ToString("N"); // No hyphens for cleaner URLs
        var cacheKey = $"{CacheKeyPrefix}{filterKey}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, criteria, cacheOptions);

        // Log filter creation without PII
        _logger.LogInformation(
            "Created filter key {FilterKey} for user {UserId}, type {FilterType}",
            filterKey,
            criteria.UserId,
            criteria.Type);

        return filterKey;
    }

    public FilterCriteria? GetFilterCriteria(string filterKey, string requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
        {
            _logger.LogWarning("Attempted to retrieve filter with null or empty key");
            return null;
        }

        var cacheKey = $"{CacheKeyPrefix}{filterKey}";

        if (!_cache.TryGetValue<FilterCriteria>(cacheKey, out var criteria))
        {
            _logger.LogWarning(
                "Filter key {FilterKey} not found or expired for user {UserId}",
                filterKey,
                requestingUserId);
            return null;
        }

        // Security validation: ensure requesting user matches the creator
        if (criteria!.UserId != requestingUserId)
        {
            _logger.LogWarning(
                "User {RequestingUserId} attempted to access filter key {FilterKey} created by {OwnerUserId}",
                requestingUserId,
                filterKey,
                criteria.UserId);
            return null;
        }

        _logger.LogDebug(
            "Retrieved filter key {FilterKey} for user {UserId}",
            filterKey,
            requestingUserId);

        return criteria;
    }

    public void RemoveFilterKey(string filterKey)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return;

        var cacheKey = $"{CacheKeyPrefix}{filterKey}";
        _cache.Remove(cacheKey);

        _logger.LogDebug("Removed filter key {FilterKey}", filterKey);
    }

    public bool ExtendFilterKey(string filterKey, string requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return false;

        var cacheKey = $"{CacheKeyPrefix}{filterKey}";

        if (!_cache.TryGetValue<FilterCriteria>(cacheKey, out var criteria))
            return false;

        // Security validation
        if (criteria!.UserId != requestingUserId)
        {
            _logger.LogWarning(
                "User {RequestingUserId} attempted to extend filter key {FilterKey} created by {OwnerUserId}",
                requestingUserId,
                filterKey,
                criteria.UserId);
            return false;
        }

        // Re-set with fresh sliding expiration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, criteria, cacheOptions);

        _logger.LogDebug(
            "Extended filter key {FilterKey} for user {UserId}",
            filterKey,
            requestingUserId);

        return true;
    }
}
