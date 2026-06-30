import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import federation from '@originjs/vite-plugin-federation';

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'mentoring',
      filename: 'remoteEntry.js',
      exposes: {
        './BrowsePage': './src/pages/BrowsePage.tsx',
        './SessionListPage': './src/pages/SessionListPage.tsx',
        './OpportunitiesPage': './src/pages/OpportunitiesPage.tsx',
      },
      shared: ['react', 'react-dom', 'react-router-dom', '@tanstack/react-query'],
    }),
  ],
  server: {
    port: 3002,
    strictPort: true,
  },
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false,
  },
});
