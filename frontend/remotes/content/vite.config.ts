import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import federation from '@originjs/vite-plugin-federation';

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'content',
      filename: 'remoteEntry.js',
      exposes: {
        './SessionPlanPage': './src/pages/SessionPlanPage.tsx',
        './AgendaTimeline': './src/components/AgendaTimeline.tsx',
        './Checklist': './src/components/Checklist.tsx',
        './ProgressBar': './src/components/ProgressBar.tsx',
      },
      shared: ['react', 'react-dom', 'react-router-dom', '@tanstack/react-query'],
    }),
  ],
  server: {
    port: 3003,
    strictPort: true,
  },
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false,
  },
});
