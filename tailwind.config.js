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
        'active-nav-link',
        'cursor-pointer'
    ],
    darkMode: 'class', // Enable class-based dark mode
    theme: {
        extend: {
            textColor: {
                'severity-medium': 'var(--color-severity-medium)',
                'severity-high': 'var(--color-severity-high)',
                'severity-extreme': 'var(--color-severity-extreme)'
            }
        },
    },
    plugins: [],
}

