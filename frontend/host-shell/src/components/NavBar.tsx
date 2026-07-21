import { useContext, useState, useEffect, useCallback } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import { usePreloadRemote } from '../hooks/usePreloadRemote';
import { REMOTE_ENTRIES } from '../lib/remote-entries';

export function NavBar() {
  const auth = useContext(AuthContext);
  const user = auth?.user;
  const isAuthenticated = auth?.isAuthenticated ?? false;
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const location = useLocation();

  // Preload handlers for federated remote routes
  const browsePreload = usePreloadRemote(REMOTE_ENTRIES['/browse']!);
  const opportunitiesPreload = usePreloadRemote(REMOTE_ENTRIES['/opportunities']!);
  const notificationsPreload = usePreloadRemote(REMOTE_ENTRIES['/notifications']!);

  // Close mobile menu on route change
  useEffect(() => {
    setMobileMenuOpen(false);
  }, [location.pathname]);

  // Close on Escape key
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      setMobileMenuOpen(false);
    }
  }, []);

  useEffect(() => {
    if (mobileMenuOpen) {
      document.addEventListener('keydown', handleKeyDown);
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
    };
  }, [mobileMenuOpen, handleKeyDown]);

  return (
    <nav
      className="flex items-center justify-between px-4 md:px-6 py-4 border-b border-border bg-bg-primary/80 backdrop-blur-md sticky top-0 z-50"
      aria-label="Main navigation"
    >
      <Link to="/" className="text-xl font-bold tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }} data-tour="dashboard">
        <span className="text-text-primary">Guided</span>
        <span className="gradient-text">Mentor</span>
      </Link>

      {/* Desktop nav */}
      {isAuthenticated && user ? (
        <div className="hidden md:flex items-center gap-4">
          {/* Notification bell */}
          <button
            className="relative p-2 rounded-lg hover:bg-white/5 transition-colors min-w-[44px] min-h-[44px] flex items-center justify-center"
            aria-label="Notifications"
            data-tour="notifications"
          >
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" className="text-text-secondary">
              <path
                d="M10 2a6 6 0 0 0-6 6v3l-1.3 2.6a1 1 0 0 0 .9 1.4h12.8a1 1 0 0 0 .9-1.4L16 11V8a6 6 0 0 0-6-6Z"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
              <path
                d="M8 15a2 2 0 1 0 4 0"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeLinecap="round"
              />
            </svg>
            <span className="absolute top-1 right-1 w-2 h-2 bg-rose rounded-full" />
          </button>

          {/* User info */}
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-bg-secondary border border-border flex items-center justify-center text-xs font-semibold text-text-primary">
              {user.displayName.split(' ').map(n => n[0]).join('')}
            </div>
            <span className="text-sm font-medium text-text-primary hidden sm:inline">
              {user.displayName}
            </span>
            <span
              className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                user.activeRole === 'mentor'
                  ? 'bg-mint/20 text-mint'
                  : 'bg-violet/20 text-violet-light'
              }`}
            >
              {user.activeRole === 'mentor' ? 'Mentor' : 'Mentee'}
            </span>
          </div>
        </div>
      ) : (
        <Link to="/login" className="hidden md:inline-flex btn-ghost text-sm">
          Sign In
        </Link>
      )}

      {/* Mobile hamburger button */}
      <button
        className="md:hidden min-w-[44px] min-h-[44px] flex items-center justify-center rounded-lg hover:bg-white/5 transition-colors"
        onClick={() => setMobileMenuOpen(true)}
        aria-label="Open menu"
        aria-expanded={mobileMenuOpen}
        aria-controls="mobile-nav-menu"
      >
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" className="text-text-primary" aria-hidden="true">
          <path d="M3 6h18M3 12h18M3 18h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        </svg>
      </button>

      {/* Mobile slide-out menu */}
      {mobileMenuOpen && (
        <div
          id="mobile-nav-menu"
          className="fixed inset-0 z-[60] md:hidden"
          role="dialog"
          aria-modal="true"
          aria-label="Mobile navigation menu"
        >
          {/* Backdrop */}
          <div
            className="absolute inset-0 bg-black/60 backdrop-blur-sm"
            onClick={() => setMobileMenuOpen(false)}
            aria-hidden="true"
          />

          {/* Menu panel */}
          <div className="absolute top-0 right-0 h-full w-72 max-w-[85vw] bg-bg-primary border-l border-border flex flex-col animate-[slideIn_200ms_ease-out]">
            {/* Close button */}
            <div className="flex items-center justify-between p-4 border-b border-border">
              <span className="text-lg font-bold tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
                <span className="text-text-primary">Guided</span>
                <span className="gradient-text">Mentor</span>
              </span>
              <button
                onClick={() => setMobileMenuOpen(false)}
                className="min-w-[44px] min-h-[44px] flex items-center justify-center rounded-lg hover:bg-white/5 transition-colors"
                aria-label="Close menu"
              >
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" className="text-text-primary" aria-hidden="true">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                </svg>
              </button>
            </div>

            {/* Nav links */}
            <div className="flex-1 overflow-y-auto p-4 space-y-1">
              {isAuthenticated && user ? (
                <>
                  {/* User info in mobile */}
                  <div className="flex items-center gap-3 p-3 mb-4 rounded-lg bg-white/5">
                    <div className="w-10 h-10 rounded-full bg-bg-secondary border border-border flex items-center justify-center text-sm font-semibold text-text-primary">
                      {user.displayName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-text-primary">{user.displayName}</p>
                      <span
                        className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                          user.activeRole === 'mentor'
                            ? 'bg-mint/20 text-mint'
                            : 'bg-violet/20 text-violet-light'
                        }`}
                      >
                        {user.activeRole === 'mentor' ? 'Mentor' : 'Mentee'}
                      </span>
                    </div>
                  </div>

                  <MobileNavLink to="/" label="Dashboard" />
                  <MobileNavLink to="/browse" label="Browse Mentors" data-tour="browse" {...browsePreload} />
                  <MobileNavLink to="/opportunities" label="Opportunities" {...opportunitiesPreload} />
                  <MobileNavLink to="/notifications" label="Notifications" {...notificationsPreload} />
                </>
              ) : (
                <>
                  <MobileNavLink to="/login" label="Sign In" />
                  <MobileNavLink to="/role-select" label="Get Started" />
                </>
              )}
            </div>

            {/* Safe area bottom spacing for notched phones */}
            <div className="safe-bottom" />
          </div>
        </div>
      )}
    </nav>
  );
}

interface MobileNavLinkProps {
  to: string;
  label: string;
  'data-tour'?: string;
  onMouseEnter?: () => void;
  onFocus?: () => void;
  onMouseLeave?: () => void;
  onBlur?: () => void;
}

function MobileNavLink({ to, label, 'data-tour': dataTour, onMouseEnter, onFocus, onMouseLeave, onBlur }: MobileNavLinkProps) {
  return (
    <Link
      to={to}
      className="flex items-center min-h-[44px] px-3 py-3 rounded-lg text-text-primary hover:bg-white/5 transition-colors text-sm font-medium"
      onMouseEnter={onMouseEnter}
      onFocus={onFocus}
      onMouseLeave={onMouseLeave}
      onBlur={onBlur}
      data-tour={dataTour}
    >
      {label}
    </Link>
  );
}
