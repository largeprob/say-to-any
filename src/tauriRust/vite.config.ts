import { reactRouter } from '@react-router/dev/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [tailwindcss(), reactRouter()],
  clearScreen: false,
  server: {
    host: '127.0.0.1',
    port: 6013,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:7001',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
    watch: {
      ignored: ['**/src-tauri/**', '**/src-tauri/target/**', '../artifacts/tauri-target/**'],
    },
  },
});
