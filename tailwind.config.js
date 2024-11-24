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
    theme: {
    extend: {},
    },
    plugins: [],
}

