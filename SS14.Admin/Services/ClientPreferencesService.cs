using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace SS14.Admin.Services;

/// <summary>
/// Handles interfacing with the client for preferences. I.E. theme preference.
/// </summary>
/// <param name="jsRuntime"></param>
public sealed class ClientPreferencesService(IJSRuntime jsRuntime)
{
    /// <summary>
    /// Raised when a client preference is changed.
    /// </summary>
    public event Action<ClientPreferences>? OnChange;

    /// <summary>
    /// Toggles the current state of dark mode, raising the appropriate event.
    /// This sets an explicit override, ignoring system preference.
    /// </summary>
    public async Task ToggleDarkMode()
    {
        var clientPreferences = await GetClientPreferences();
        var newClientPreferences = new ClientPreferences{darkMode = !clientPreferences.darkMode};
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "darkModeOverride", newClientPreferences.darkMode ? "true" : "false");
        OnChange?.Invoke(newClientPreferences);
    }

    /// <summary>
    /// Gets the client's preferences.
    /// </summary>
    /// <returns></returns>
    public async Task<ClientPreferences> GetClientPreferences()
    {
        var clientPreferences = await jsRuntime.InvokeAsync<ClientPreferences>("getClientPreferences");

        return clientPreferences;
    }

    /// <summary>
    /// JSON struct for passing the client preferences between the client and server.
    /// </summary>
    public record struct ClientPreferences
    {
        // FIXME: It doesn't marshal for some reason if I use JsonPropertyName
        public bool darkMode { get; set; }
    };
}
