/**
 * Gets the client's preferences, i.e. dark mode.
 * @returns {ClientPreferences}
 */
window.getClientPreferences = () => {
    let darkModeOverride = localStorage.getItem("darkModeOverride");
    let darkMode;

    if (darkModeOverride === null) {
        // No override set, use system preference
        darkMode = window.matchMedia("(prefers-color-scheme: dark)").matches;
    } else {
        // User has explicitly set a preference
        darkMode = darkModeOverride === "true";
    }

    return {
        darkMode
    };
}
