import { BrowsePage } from './pages/BrowsePage';
import { SessionListPage } from './pages/SessionListPage';
import { OpportunitiesPage } from './pages/OpportunitiesPage';

/**
 * Standalone dev mode entry — shows the browse page by default.
 * In production, individual pages are exposed via Module Federation.
 */
function App() {
  // Simple hash-based routing for standalone dev mode
  const hash = typeof window !== 'undefined' ? window.location.hash : '';

  if (hash === '#/sessions') return <SessionListPage />;
  if (hash === '#/opportunities') return <OpportunitiesPage />;
  return <BrowsePage />;
}

export default App;
