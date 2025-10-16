using Microsoft.EntityFrameworkCore;
using SS14.Admin.Data;
using SS14.Admin.Data.Models;

namespace SS14.Admin.Services;

/// <summary>
/// Dummy implementation of user preferences service.
/// WIP
/// - Store preferences in SQLite database
/// - Sync with ClientPreferencesService
/// - Handle user-specific preferences
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly PreferencesDbContext _context;

    public UserPreferencesService(PreferencesDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<UserPreference?> GetUserPreferencesAsync(string userId)
    {
        //dummy implementation
        return await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    /// <inheritdoc/>
    public async Task SaveUserPreferencesAsync(UserPreference preference)
    {
        // dummy implementation
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId);

        if (existing != null)
        {
            existing.DarkModeOverride = preference.DarkModeOverride;
            existing.AdditionalPreferences = preference.AdditionalPreferences;
            existing.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            preference.LastUpdated = DateTime.UtcNow;
            _context.UserPreferences.Add(preference);
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateDarkModePreferenceAsync(string userId, bool? darkModeOverride)
    {
        // dummy implementation
        var preference = await GetUserPreferencesAsync(userId);

        if (preference != null)
        {
            preference.DarkModeOverride = darkModeOverride;
            preference.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            preference = new UserPreference
            {
                UserId = userId,
                DarkModeOverride = darkModeOverride,
                LastUpdated = DateTime.UtcNow
            };
            _context.UserPreferences.Add(preference);
        }

        await _context.SaveChangesAsync();
    }
}
