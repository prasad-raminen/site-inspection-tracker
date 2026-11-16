/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// In development the Vite server proxies /api and /health to the ASP.NET Core API,
// so the client can use relative URLs and no CORS round-trip is needed.
const apiTarget = process.env.VITE_DEV_API_PROXY ?? 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': apiTarget,
      '/health': apiTarget,
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
    css: false,
  },
});
