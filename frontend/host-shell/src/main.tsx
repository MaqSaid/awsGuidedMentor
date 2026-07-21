import { StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './providers/AuthProvider';
import { RoleProvider } from './providers/RoleProvider';
import { ToastProvider } from './components/Toast';
import App from './App';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      retry: 1,
    },
  },
});

async function enableMocking() {
  if (import.meta.env.VITE_DISABLE_MOCKS === 'true') {
    return;
  }
  const { worker } = await import('./mocks/browser');
  await worker.start({ onUnhandledRequest: 'bypass' });

  // Auto-login as James Okonkwo (mentee) for demo
  const mockUser = {
    userId: 'user-mentee-0001',
    email: 'james.okonkwo@guidedmentor.dev',
    displayName: 'James Okonkwo',
    profilePhotoUrl: null,
    activeRole: 'mentee',
  };
  const header = btoa(JSON.stringify({ alg: 'RS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({
    sub: mockUser.userId,
    email: mockUser.email,
    exp: Math.floor(Date.now() / 1000) + 3600,
  }));
  const fakeToken = `${header}.${payload}.mock-signature`;

  localStorage.setItem('gm_access_token', fakeToken);
  localStorage.setItem('gm_refresh_token', 'mock-refresh-token');
  localStorage.setItem('gm_user', JSON.stringify(mockUser));
}

enableMocking().then(() => {
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider>
            <RoleProvider>
              <ToastProvider>
                <App />
              </ToastProvider>
            </RoleProvider>
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </StrictMode>,
  );

  // Register Service Worker for offline support (production only)
  if ('serviceWorker' in navigator && import.meta.env.PROD) {
    window.addEventListener('load', () => {
      navigator.serviceWorker.register('/sw.js').catch(() => {
        // Silent fail — SW is a progressive enhancement
      });
    });
  }
});
