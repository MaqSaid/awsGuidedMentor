import { useState, useCallback, useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import { apiUrl } from '../lib/api';

type LoginState = 'idle' | 'loading' | 'sent' | 'error';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [state, setState] = useState<LoginState>('idle');
  const [error, setError] = useState('');

  const handleSendMagicLink = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.includes('@')) {
      setError('Please enter a valid email address.');
      setState('error');
      return;
    }

    setState('loading');
    setError('');

    try {
      const response = await fetch(apiUrl('/v1/auth/magic-link'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
      });

      if (response.ok) {
        setState('sent');
      } else {
        setState('error');
        setError('Something went wrong. Please try again.');
      }
    } catch {
      setState('error');
      setError('Network error. Please check your connection and try again.');
    }
  }, [email]);

  const handleGoogleSignIn = useCallback(() => {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    const redirectUri = import.meta.env.VITE_GOOGLE_REDIRECT_URI || `${window.location.origin}/auth/google/callback`;
    const scope = 'openid email profile';
    const url = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=${encodeURIComponent(scope)}`;
    window.location.href = url;
  }, []);

  return (
    <div className="relative min-h-screen flex items-center justify-center px-4">
      {/* Decorative glow orbs */}
      <div className="absolute top-20 left-1/4 w-96 h-96 rounded-full bg-violet/20 blur-[128px] pointer-events-none" />
      <div className="absolute bottom-20 right-1/4 w-80 h-80 rounded-full bg-mint/20 blur-[128px] pointer-events-none" />

      <div className="relative z-10 w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <Link to="/" className="inline-block text-2xl font-bold tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
            <span className="text-text-primary">Guided</span>
            <span className="gradient-text">Mentor</span>
          </Link>
        </div>

        <div className="glass-card p-8">
          <h1
            className="text-2xl font-bold text-text-primary text-center mb-2"
            style={{ fontFamily: 'Outfit, sans-serif' }}
          >
            Sign In
          </h1>
          <p className="text-text-secondary text-center text-sm mb-8">
            No password needed — we&apos;ll send you a sign-in link
          </p>

          {state === 'sent' ? (
            <div className="text-center" role="status" aria-live="polite">
              <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-mint/20 flex items-center justify-center">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M3 8l9 6 9-6" stroke="var(--color-mint)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                  <rect x="2" y="5" width="20" height="14" rx="2" stroke="var(--color-mint)" strokeWidth="2" />
                </svg>
              </div>
              <h2 className="text-lg font-semibold text-text-primary mb-2">Check your inbox!</h2>
              <p className="text-text-secondary text-sm">
                We sent a sign-in link to <span className="text-text-primary font-medium">{email}</span>
              </p>
              <p className="text-text-secondary text-xs mt-4">
                The link expires in 10 minutes. Check your spam folder if you don&apos;t see it.
              </p>
            </div>
          ) : (
            <>
              {/* Google OAuth */}
              <button
                type="button"
                onClick={handleGoogleSignIn}
                className="w-full flex items-center justify-center gap-3 px-4 py-3 rounded-lg border border-white/10 bg-white/5 text-text-primary font-medium transition-colors hover:bg-white/10 focus:outline-none focus:ring-2 focus:ring-violet/50"
              >
                <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
                  <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
                  <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
                  <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
                </svg>
                Sign in with Google
              </button>

              {/* Divider */}
              <div className="flex items-center gap-4 my-6">
                <div className="flex-1 h-px bg-white/10" />
                <span className="text-text-secondary text-xs uppercase tracking-wider">or</span>
                <div className="flex-1 h-px bg-white/10" />
              </div>

              {/* Email magic link form */}
              <form onSubmit={handleSendMagicLink}>
                <label htmlFor="email" className="block text-sm font-medium text-text-secondary mb-2">
                  Email address
                </label>
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => { setEmail(e.target.value); setError(''); setState('idle'); }}
                  placeholder="you@example.com"
                  required
                  autoComplete="email"
                  className="w-full px-4 py-3 rounded-lg bg-white/5 border border-white/10 text-text-primary placeholder:text-text-secondary/50 focus:outline-none focus:ring-2 focus:ring-violet/50 focus:border-transparent transition-colors"
                  aria-describedby={error ? 'email-error' : undefined}
                  aria-invalid={state === 'error' ? 'true' : undefined}
                />

                {state === 'error' && error && (
                  <p id="email-error" className="mt-2 text-sm text-rose" role="alert" aria-live="polite">
                    {error}
                  </p>
                )}

                <button
                  type="submit"
                  disabled={state === 'loading'}
                  className="w-full mt-4 btn-violet py-3 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {state === 'loading' ? (
                    <span className="flex items-center justify-center gap-2">
                      <span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                      Sending...
                    </span>
                  ) : (
                    'Send magic link'
                  )}
                </button>
              </form>
            </>
          )}
        </div>

        {/* Back to landing */}
        <p className="text-center mt-6 text-text-secondary text-sm">
          <Link to="/" className="text-violet-light hover:text-violet transition-colors">
            &larr; Back to home
          </Link>
        </p>

        {/* Dev Quick Login — only visible in development */}
        {import.meta.env.DEV && <DevQuickLogin />}
      </div>
    </div>
  );
}

/** Dev-only component for instant login as seeded users */
function DevQuickLogin() {
  const auth = useContext(AuthContext);
  const navigate = useNavigate();

  const devUsers = [
    { id: '11111111-1111-1111-1111-111111111111', name: 'Sarah Chen', role: 'mentor', chapter: 'Sydney' },
    { id: '22222222-2222-2222-2222-222222222222', name: 'James Nguyen', role: 'mentor', chapter: 'Melbourne' },
    { id: '33333333-3333-3333-3333-333333333333', name: 'Priya Sharma', role: 'both', chapter: 'Brisbane' },
    { id: '44444444-4444-4444-4444-444444444444', name: 'David Kim', role: 'mentor', chapter: 'Perth' },
    { id: '66666666-6666-6666-6666-666666666666', name: 'Alex Patel', role: 'mentee', chapter: 'Sydney' },
    { id: '77777777-7777-7777-7777-777777777777', name: 'Mia Johnson', role: 'mentee', chapter: 'Melbourne' },
    { id: '88888888-8888-8888-8888-888888888888', name: 'Liam Brown', role: 'mentee', chapter: 'Brisbane' },
  ];

  const handleDevLogin = (user: typeof devUsers[0]) => {
    // Create a fake JWT-like token for dev auth
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const payload = btoa(JSON.stringify({
      sub: user.id,
      email: `${user.name.toLowerCase().replace(' ', '.')}@example.com`,
      name: user.name,
      role: user.role === 'both' ? 'mentor' : user.role,
      exp: Math.floor(Date.now() / 1000) + 3600, // 1 hour
    }));
    const fakeToken = `${header}.${payload}.dev-signature`;

    auth?.login(fakeToken, 'dev-refresh-token', {
      userId: user.id,
      email: `${user.name.toLowerCase().replace(' ', '.')}@example.com`,
      displayName: user.name,
      profilePhotoUrl: null,
      activeRole: user.role === 'both' ? 'mentor' : user.role as 'mentor' | 'mentee',
    });

    navigate('/');
  };

  return (
    <div className="mt-8 glass-card p-4">
      <p className="text-xs text-amber font-semibold mb-3 text-center uppercase tracking-wider">
        Dev Quick Login
      </p>
      <div className="grid grid-cols-1 gap-2">
        {devUsers.map((user) => (
          <button
            key={user.id}
            onClick={() => handleDevLogin(user)}
            className="flex items-center justify-between px-3 py-2 rounded-lg bg-white/5 hover:bg-white/10 transition-colors text-left min-h-[44px]"
          >
            <span className="text-sm text-text-primary">{user.name}</span>
            <span className={`text-xs px-2 py-0.5 rounded-full ${
              user.role === 'mentor' ? 'bg-mint/20 text-mint' :
              user.role === 'mentee' ? 'bg-violet/20 text-violet-light' :
              'bg-amber/20 text-amber'
            }`}>
              {user.role} • {user.chapter}
            </span>
          </button>
        ))}
      </div>
      <p className="text-xs text-text-muted mt-2 text-center">
        These buttons only appear in development mode.
      </p>
    </div>
  );
}
