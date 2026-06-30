import { useContext } from 'react';
import { Link } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';

export function NavBar() {
  const auth = useContext(AuthContext);
  const user = auth?.user;
  const isAuthenticated = auth?.isAuthenticated ?? false;

  return (
    <nav
      className="flex items-center justify-between px-6 py-4 border-b border-border bg-bg-primary/80 backdrop-blur-md sticky top-0 z-50"
      aria-label="Main navigation"
    >
      <Link to="/" className="text-xl font-bold tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
        <span className="text-text-primary">Guided</span>
        <span className="gradient-text">Mentor</span>
      </Link>

      {isAuthenticated && user ? (
        <div className="flex items-center gap-4">
          {/* Notification bell */}
          <button
            className="relative p-2 rounded-lg hover:bg-white/5 transition-colors"
            aria-label="Notifications"
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
        <Link to="/login" className="btn-ghost text-sm">
          Sign In
        </Link>
      )}
    </nav>
  );
}
