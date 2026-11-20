/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ink: {
          950: '#04060c',
          900: '#060a13',
          850: '#080e1a',
          800: '#0b1322',
          700: '#121c2f',
          600: '#1b2942'
        },
        gold: {
          200: '#f6e7b4',
          300: '#efd794',
          400: '#e3c06a',
          500: '#d4af37',
          600: '#b8942a'
        },
        cyanx: {
          300: '#67e8f9',
          400: '#22d3ee',
          500: '#06b6d4'
        }
      },
      fontFamily: {
        display: ['Sora', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        sans: ['Manrope', 'ui-sans-serif', 'system-ui', 'sans-serif']
      },
      boxShadow: {
        luxury: '0 24px 80px -32px rgba(212, 175, 55, 0.35)',
        glow: '0 0 40px -8px rgba(34, 211, 238, 0.35)',
        card: '0 20px 60px -30px rgba(0, 0, 0, 0.8)'
      },
      backgroundImage: {
        'gold-gradient': 'linear-gradient(135deg, #f6e7b4 0%, #d4af37 45%, #b8942a 100%)',
        'panel-gradient': 'linear-gradient(160deg, rgba(255,255,255,0.07), rgba(255,255,255,0.01))'
      },
      keyframes: {
        shimmer: {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' }
        },
        pulseSoft: {
          '0%, 100%': { opacity: '0.55' },
          '50%': { opacity: '1' }
        },
        floatY: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-10px)' }
        }
      },
      animation: {
        shimmer: 'shimmer 2.2s linear infinite',
        'pulse-soft': 'pulseSoft 2.4s ease-in-out infinite',
        float: 'floatY 6s ease-in-out infinite'
      }
    }
  },
  plugins: []
}
