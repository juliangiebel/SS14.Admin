/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
      './Components/**/*.razor', //Blazor
      './Pages/**/*.{razor,html}', //Razor Pages
      './Shared/**/*.{razor,html}', //Shared Componets
      './wwwroot/**/*.{razor,html}', //Static html
    ],
    safelist: [
        'active-nav-link'
    ],
    darkMode: 'class', // Enable class-based dark mode
    theme: {
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
    },
    plugins: [],
}

