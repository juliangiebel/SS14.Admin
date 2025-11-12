using SS14.Admin.Models;

namespace SS14.Admin.Services;

/// <summary>
/// Service for managing opaque filter keys that prevent PII leakage in URLs.
/// Filter keys are short-lived tokens that reference server-side stored filter criteria.
/// </summary>
public interface IFilterKeyService
{
    /// <summary>
    /// Creates a new filter key for the given criteria.
    /// The filter is stored server-side and associated with the requesting user.
    /// </summary>
    /// <param name="criteria">Filter criteria (may contain PII)</param>
    /// <returns>Opaque GUID-based filter key</returns>
    string CreateFilterKey(FilterCriteria criteria);

    /// <summary>
    /// Retrieves filter criteria by key, validating that the requesting user matches.
    /// </summary>
    /// <param name="filterKey">The opaque filter key</param>
    /// <param name="requestingUserId">The user ID making the request (from claims)</param>
    /// <returns>Filter criteria if found and valid, null otherwise</returns>
    FilterCriteria? GetFilterCriteria(string filterKey, string requestingUserId);

    /// <summary>
    /// Removes a filter key from storage (for manual cleanup).
    /// </summary>
    /// <param name="filterKey">The filter key to remove</param>
    void RemoveFilterKey(string filterKey);

    /// <summary>
    /// Extends the expiration of a filter key (sliding expiration on access).
    /// </summary>
    /// <param name="filterKey">The filter key to extend</param>
    /// <param name="requestingUserId">The user ID making the request</param>
    /// <returns>True if extended, false if not found or invalid user</returns>
    bool ExtendFilterKey(string filterKey, string requestingUserId);
}
