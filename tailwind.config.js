/** @type {import('tailwindcss').Config} */
/** Ima be completly real i have no idea if the colors section works or not,
 * but it kinda works for darkmode so... ¯\_(ツ)_/ - Geeky*/
module.exports = {
    content: [
      './Components/**/*.{razor,css}', //Blazor, adding css to all of these should allow for scoped css on a component bases.
      './Pages/**/*.{razor,html}', //Razor Pages
      './Shared/**/*.{razor,html}', //Shared Componets
      './wwwroot/**/*.{razor,html,css}', //Static html
    ],
    safelist: [
        'active-nav-link'
    ],
    darkMode: 'class', // Enable class-based dark mode
    /*theme: {
        extend: {
            colors: {
                background: 'var(--color-background)',
                surface: 'var(--color-surface)',
                border: 'var(--color-border)',
                textPrimary: 'var(--color-text-primary)',
                textSecondary: 'var(--color-text-secondary)',
                errorBg: 'var(--color-error-bg)',
                errorText: 'var(--color-error-text)',
                link: 'var(--color-link)',
            },
        },
    },*/
    plugins: [],
}

