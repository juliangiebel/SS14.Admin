/**
 * Gets the client's preferences, i.e. dark mode.
 * @returns {ClientPreferences}
 */
window.getClientPreferences = () => {
    let darkModeString = localStorage.getItem("darkMode");
    let darkMode;
    if (darkModeString === null) {
        darkMode = window.matchMedia("(prefers-color-scheme: dark-mode)").matches;
    } else {
        darkMode = darkModeString === "true";
    }

    return {
        darkMode
    };
}
