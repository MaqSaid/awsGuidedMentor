import { useEffect, useState, useContext, useCallback } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';

type VerifyState = 'verifying' | 'success' | 'error';

export default function VerifyMagicLink() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const auth = useContext(AuthContext);
  const [state, setState] = useState<VerifyState>('verifying');
  const [error, setError] = useState('');

  const token = searchParams.get('token');
  const email = searchParams.get('email');

  const verifyToken = useCallback(async () => {
    if (!token || !email) {
      setState('error');
      setError('Invalid or expired link. Please request a new sign-in link.');
      return;
    }

    try {
      const response = await fetch('/v1/auth/verify-magic-link', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, token }),
      });

      if (response.ok) {
        const data = await response.json() as {
          accessToken: string;
          refreshToken: string;
          idToken: string;
          activeRole: string | null;
          expiresIn: number;
        };

        // Parse user info from ID token
        const payload = JSON.parse(atob(data.idToken.split('.')[1]!));
        auth?.login(data.accessToken, data.refreshToken, {
          userId: payload.sub,
          email: payload.email || email,
          displayName: payload.name || email.split('@')[0]!,
          profilePhotoUrl: null,
          activeRole: data.activeRole as 'mentor' | 'mentee' | null,
        });

        setState('success');

        // Redirect after brief success state
        setTimeout(() => {
          if (data.activeRole) {
            navigate('/dashboard', { replace: true });
          } else {
            navigate('/role-select', { replace: true });
          }
        }, 1500);
      } else {
        const errorData = await response.json().catch(() => ({ error: 'Verification failed.' })) as { error: string };
        setState('error');
        setError(errorData.error || 'This link is invalid or has expired. Please request a new one.');
      }
    } catch {
      setState('error');
      setError('Network error. Please check your connection and try again.');
    }
  }, [token, email, auth, navigate]);

  useEffect(() => {
    void verifyToken();
  }, [verifyToken]);

  return (
    <div className="relative min-h-screen flex items-center justify-center px-4">
      {/* Decorative glow */}
      <div className="absolute top-20 left-1/4 w-96 h-96 rounded-full bg-violet/20 blur-[128px] pointer-events-none" />

      <div className="relative z-10 w-full max-w-md text-center">
        <div className="glass-card p-8">
          {state === 'verifying' && (
            <div role="status" aria-live="polite">
              <div className="w-12 h-12 mx-auto mb-4 border-2 border-violet border-t-transparent rounded-full animate-spin" />
              <h1 className="text-xl font-semibold text-text-primary mb-2" style={{ fontFamily: 'Outfit, sans-serif' }}>
                Verifying your link...
              </h1>
              <p className="text-text-secondary text-sm">Just a moment while we sign you in.</p>
            </div>
          )}

          {state === 'success' && (
            <div role="status" aria-live="polite">
              <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-mint/20 flex items-center justify-center">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M5 13l4 4L19 7" stroke="var(--color-mint)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <h1 className="text-xl font-semibold text-text-primary mb-2" style={{ fontFamily: 'Outfit, sans-serif' }}>
                You&apos;re signed in!
              </h1>
              <p className="text-text-secondary text-sm">Redirecting you now...</p>
            </div>
          )}

          {state === 'error' && (
            <div role="alert" aria-live="assertive">
              <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-rose/20 flex items-center justify-center">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M18 6L6 18M6 6l12 12" stroke="var(--color-rose)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <h1 className="text-xl font-semibold text-text-primary mb-2" style={{ fontFamily: 'Outfit, sans-serif' }}>
                Verification failed
              </h1>
              <p className="text-text-secondary text-sm mb-6">{error}</p>
              <Link to="/login" className="btn-violet inline-block px-6 py-3">
                Request a new link
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
