/**
 * Engagement Remote — Standalone dev mode app with routing.
 * In production, these components are loaded via Module Federation.
 */
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { MenteeDashboard } from './pages/MenteeDashboard';
import { MentorDashboard } from './pages/MentorDashboard';
import { NotificationPanel } from './pages/NotificationPanel';
import { AIHelpAssistant } from './pages/AIHelpAssistant';
import { OnboardingTour } from './pages/OnboardingTour';
import { MeetupCalendar } from './pages/MeetupCalendar';
import { OperatorAnalytics } from './pages/OperatorAnalytics';
import { TrackerProvider, ConsentBanner } from './tracking';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 2,
      refetchOnWindowFocus: false,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <TrackerProvider>
        <BrowserRouter>
          <div className="min-h-screen bg-background text-text-primary">
            <Routes>
              <Route path="/" element={<MenteeDashboard />} />
              <Route path="/dashboard/mentee" element={<MenteeDashboard />} />
              <Route path="/dashboard/mentor" element={<MentorDashboard />} />
              <Route path="/notifications" element={<NotificationPanel />} />
              <Route path="/meetups" element={<MeetupCalendar />} />
              <Route path="/admin/analytics" element={<OperatorAnalytics />} />
            </Routes>

            {/* Global overlays */}
            <AIHelpAssistant />
            <OnboardingTour />
            <ConsentBanner />
          </div>
        </BrowserRouter>
      </TrackerProvider>
    </QueryClientProvider>
  );
}

export default App;
