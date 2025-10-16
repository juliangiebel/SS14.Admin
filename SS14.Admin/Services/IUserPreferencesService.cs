using SS14.Admin.Data.Models;

namespace SS14.Admin.Services;

/// <summary>
/// Interface for user preferences service.
/// This is a dummy implementation not used
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets user preferences from the database.
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>User preferences or null if not found</returns>
    Task<UserPreference?> GetUserPreferencesAsync(string userId);

    /// <summary>
    /// Saves or updates user preferences in the database.
    /// </summary>
    /// <param name="preference">The user preference to save</param>
    Task SaveUserPreferencesAsync(UserPreference preference);

    /// <summary>
    /// Updates the dark mode preference for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="darkModeOverride">Dark mode override (null = use system preference)</param>
    Task UpdateDarkModePreferenceAsync(string userId, bool? darkModeOverride);
}
