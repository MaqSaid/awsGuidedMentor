import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import federation from '@originjs/vite-plugin-federation';

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'engagement',
      filename: 'remoteEntry.js',
      exposes: {
        './MenteeDashboard': './src/pages/MenteeDashboard.tsx',
        './MentorDashboard': './src/pages/MentorDashboard.tsx',
        './NotificationPanel': './src/pages/NotificationPanel.tsx',
        './AIHelpAssistant': './src/pages/AIHelpAssistant.tsx',
        './OnboardingTour': './src/pages/OnboardingTour.tsx',
        './MeetupCalendar': './src/pages/MeetupCalendar.tsx',
        './OperatorAnalytics': './src/pages/OperatorAnalytics.tsx',
        './ConsentBanner': './src/tracking/ConsentBanner.tsx',
        './TrackerProvider': './src/tracking/TrackerProvider.tsx',
      },
      shared: ['react', 'react-dom', 'react-router-dom', '@tanstack/react-query'],
    }),
  ],
  server: {
    port: 3004,
    strictPort: true,
  },
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false,
  },
});
