import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SessionPlanPage } from './pages/SessionPlanPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <div className="min-h-screen bg-[var(--color-background)]">
          <h1 className="p-4 text-lg font-semibold text-[var(--color-text-primary)]">
            Content Remote - Standalone Dev Mode
          </h1>
          <Routes>
            <Route path="/sessions/:sessionId" element={<SessionPlanPage />} />
            <Route path="*" element={<SessionPlanPage />} />
          </Routes>
        </div>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
