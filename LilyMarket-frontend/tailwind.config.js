/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: '#0F0F0F',
        card: '#1A1A1A',
        accent: '#D4AF37',
        text: '#F5F5F5',
        secondary: '#A0A0A0',
        error: '#DC2626',
        success: '#22C55E',
      },
      borderRadius: {
        'card': '16px',
        'button': '9999px',
      },
      animation: {
        'fadeIn': 'fadeIn 0.3s ease-out',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(10px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
      },
    },
  },
  plugins: [],
}