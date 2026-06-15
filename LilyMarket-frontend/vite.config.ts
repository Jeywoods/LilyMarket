import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',  // Слушать все интерфейсы
    port: 3000,
    strictPort: false,
    proxy: {
      '/api': {
        target: 'http://localhost:5079',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:5079',
        changeOrigin: true,
        ws: true
      }
    }
  }
})