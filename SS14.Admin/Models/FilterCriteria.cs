namespace SS14.Admin.Models;

/// <summary>
/// Represents opaque filter criteria that can contain PII.
/// Stored server-side and referenced via a filterKey to prevent PII leakage in URLs.
/// </summary>
public class FilterCriteria
{
    /// <summary>
    /// The user ID who created this filter
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Type of filter for routing purposes
    /// </summary>
    public required FilterType Type { get; init; }

    /// <summary>
    /// Generic search term (may contain PII like IP, HWID, username)
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Date range start
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Server ID filter
    /// </summary>
    public int? ServerId { get; init; }

    /// <summary>
    /// Player/User ID filter
    /// </summary>
    public Guid? UserId_Filter { get; init; }

    /// <summary>
    /// Connection type filters (for connections page)
    /// </summary>
    public ConnectionTypeFilters? ConnectionTypes { get; init; }

    /// <summary>
    /// Additional filter properties stored as key-value pairs
    /// </summary>
    public Dictionary<string, object>? AdditionalFilters { get; init; }

    /// <summary>
    /// Timestamp when this filter was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of filter for routing/page determination
/// </summary>
public enum FilterType
{
    Connections,
    Players,
    Bans,
    RoleBans,
    Characters,
    Logs,
    Whitelist
}

/// <summary>
/// Connection type filters for Connections page
/// </summary>
public class ConnectionTypeFilters
{
    public bool ShowAccepted { get; init; } = true;
    public bool ShowBanned { get; init; } = true;
    public bool ShowWhitelist { get; init; } = true;
    public bool ShowFull { get; init; } = true;
    public bool ShowPanic { get; init; } = true;
    public bool ShowBabyJail { get; init; } = true;
    public bool ShowIPChecks { get; init; } = true;
}
