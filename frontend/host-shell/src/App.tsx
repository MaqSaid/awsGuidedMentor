import { lazy, Suspense, useContext } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthContext } from './providers/AuthProvider';
import { NavBar } from './components/NavBar';
import LandingPage from './pages/LandingPage';

// Lazy-loaded routes (code-split per page)
const RoleSelector = lazy(() => import('./pages/RoleSelector'));
const OnboardingWizard = lazy(() => import('./pages/OnboardingWizard'));
const MenteeDashboard = lazy(() => import('./pages/MenteeDashboard'));
const MentorDashboard = lazy(() => import('./pages/MentorDashboard'));
const BrowseMentors = lazy(() => import('./pages/BrowseMentors'));
const SessionPlan = lazy(() => import('./pages/SessionPlan'));

function PageLoader() {
  return (
    <div className="flex items-center justify-center min-h-[50vh]" role="status">
      <div className="flex flex-col items-center gap-3">
        <div className="w-8 h-8 border-2 border-violet border-t-transparent rounded-full animate-spin" />
        <span className="text-text-secondary text-sm">Loading...</span>
      </div>
    </div>
  );
}

function DashboardRouter() {
  const auth = useContext(AuthContext);
  const role = auth?.user?.activeRole;

  if (role === 'mentor') return <MentorDashboard />;
  return <MenteeDashboard />;
}

function HomeRouter() {
  const auth = useContext(AuthContext);
  if (auth?.isAuthenticated) return <DashboardRouter />;
  return <LandingPage />;
}

function App() {
  const auth = useContext(AuthContext);
  const showNav = auth?.isAuthenticated ?? false;

  return (
    <>
      {/* Skip-nav link for accessibility */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-[100] focus:px-4 focus:py-2 focus:bg-violet focus:text-white focus:rounded-lg"
      >
        Skip to main content
      </a>

      {showNav && (
        <header>
          <NavBar />
        </header>
      )}

      <main id="main-content">
        <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/" element={<HomeRouter />} />
            <Route path="/role-select" element={<RoleSelector />} />
            <Route path="/onboarding" element={<OnboardingWizard />} />
            <Route path="/dashboard" element={<DashboardRouter />} />
            <Route path="/browse" element={<BrowseMentors />} />
            <Route path="/sessions/:id/plan" element={<SessionPlan />} />
            <Route path="/login" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </main>
    </>
  );
}

export default App;
