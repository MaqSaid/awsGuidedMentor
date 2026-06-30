import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import federation from '@originjs/vite-plugin-federation';

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'identity',
      filename: 'remoteEntry.js',
      exposes: {
        './LoginPage': './src/pages/LoginPage.tsx',
        './SignupPage': './src/pages/SignupPage.tsx',
        './OnboardingWizard': './src/pages/OnboardingWizard.tsx',
        './SettingsPage': './src/pages/SettingsPage.tsx',
      },
      shared: ['react', 'react-dom', 'react-router-dom', '@tanstack/react-query'],
    }),
  ],
  server: {
    port: 3001,
    strictPort: true,
  },
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false,
  },
});
