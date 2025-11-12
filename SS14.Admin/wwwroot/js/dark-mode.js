/**
 * Gets the client's preferences, i.e. dark mode and PII censoring.
 * yes i know this is on darkmode.js or whatever ill refactor it sometime
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

    // Get PII censoring preference (defaults to false if not set)
    let censorPii = localStorage.getItem("censorPii") === "true";

    return {
        darkMode,
        censorPii
    };
}
