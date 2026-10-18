import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// En desarrollo, Vite en http://localhost:5173 y la API .NET en https://localhost:5001.
// secure: false porque la API usa el certificado de desarrollo de .NET.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'https://localhost:5001', changeOrigin: true, secure: false },
      '/salud': { target: 'https://localhost:5001', changeOrigin: true, secure: false },
    },
  },
})
