namespace SS14.Admin.Data.Models;

/// <summary>
/// Represents user preferences stored in the local SQLite database.
/// This is a dummy implementation for future use.
/// </summary>
public class UserPreference
{
    /// <summary>
    /// Primary key - User's unique identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Dark mode preference override (null = use system, true/false = explicit override)
    /// </summary>
    public bool? DarkModeOverride { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional preferences stored as JSON for future extensibility
    /// </summary>
    public string? AdditionalPreferences { get; set; }
}
