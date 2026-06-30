import { useState, useCallback, type FormEvent } from 'react';
import { Button } from '@guided-mentor/design-system';
import { Input } from '@guided-mentor/design-system';
import { Skeleton } from '@guided-mentor/design-system';

/**
 * LoginPage — Google OAuth button + email/password form.
 * Generic error messages only (no field-specific hints).
 * Account lockout message with countdown.
 *
 * Requirements: 1.1, 1.2, 13.1, 13.2, 13.3
 */

interface LoginState {
  email: string;
  password: string;
  loading: boolean;
  error: string | null;
  lockoutMinutes: number | null;
}

export function LoginPage() {
  const [state, setState] = useState<LoginState>({
    email: '',
    password: '',
    loading: false,
    error: null,
    lockoutMinutes: null,
  });

  const handleGoogleOAuth = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }));
    try {
      // Redirect to Google OAuth via Cognito hosted UI
      const cognitoDomain = import.meta.env.VITE_COGNITO_DOMAIN ?? '';
      const clientId = import.meta.env.VITE_COGNITO_CLIENT_ID ?? '';
      const redirectUri = import.meta.env.VITE_OAUTH_REDIRECT_URI ?? window.location.origin + '/auth/callback';
      const url = `${cognitoDomain}/oauth2/authorize?identity_provider=Google&response_type=code&client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&scope=openid+email+profile`;
      window.location.href = url;
    } catch {
      setState((prev) => ({ ...prev, loading: false, error: 'Unable to connect. Please try again.' }));
    }
  }, []);

  const handleSubmit = useCallback(
    async (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      setState((prev) => ({ ...prev, loading: true, error: null, lockoutMinutes: null }));

      try {
        const response = await fetch('/v1/auth/signin', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email: state.email, password: state.password }),
        });

        if (response.status === 423) {
          const data = (await response.json()) as { lockoutMinutes?: number };
          setState((prev) => ({
            ...prev,
            loading: false,
            lockoutMinutes: data.lockoutMinutes ?? 15,
            error: null,
          }));
          return;
        }

        if (!response.ok) {
          setState((prev) => ({
            ...prev,
            loading: false,
            error: 'Email or password is incorrect',
          }));
          return;
        }

        // Success — redirect to role check or dashboard
        window.location.href = '/';
      } catch {
        setState((prev) => ({
          ...prev,
          loading: false,
          error: 'Unable to connect. Please try again.',
        }));
      }
    },
    [state.email, state.password]
  );

  // Initial loading skeleton
  if (state.loading && !state.email && !state.password) {
    return (
      <div className="flex items-center justify-center min-h-screen" data-testid="identity-login-page">
        <div className="glass-card p-8 w-full max-w-md space-y-6">
          <Skeleton height="2rem" width="60%" />
          <Skeleton height="3rem" />
          <Skeleton height="1rem" width="100%" />
          <Skeleton height="3rem" />
          <Skeleton height="3rem" />
          <Skeleton height="2.5rem" />
        </div>
      </div>
    );
  }

  return (
    <div
      className="flex items-center justify-center min-h-screen px-4"
      data-testid="identity-login-page"
    >
      <div className="glass-card p-8 w-full max-w-md space-y-6">
        {/* Header */}
        <div className="text-center space-y-2">
          <h1 className="text-2xl font-bold text-text-primary">Welcome Back</h1>
          <p className="text-sm text-text-secondary">
            Sign in to GuidedMentor
          </p>
        </div>

        {/* Google OAuth Button */}
        <Button
          variant="secondary"
          size="lg"
          className="w-full gap-3"
          onClick={handleGoogleOAuth}
          loading={state.loading}
          aria-label="Sign in with Google"
        >
          <svg
            className="h-5 w-5"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
              fill="#4285F4"
            />
            <path
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
              fill="#34A853"
            />
            <path
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
              fill="#FBBC05"
            />
            <path
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
              fill="#EA4335"
            />
          </svg>
          Sign in with Google
        </Button>

        {/* Divider */}
        <div className="flex items-center gap-4">
          <div className="flex-1 h-px bg-[rgba(255,255,255,0.08)]" />
          <span className="text-xs text-text-muted uppercase tracking-wide">or</span>
          <div className="flex-1 h-px bg-[rgba(255,255,255,0.08)]" />
        </div>

        {/* Email/Password Form */}
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
          {/* Lockout Message */}
          {state.lockoutMinutes !== null && (
            <div
              role="alert"
              aria-live="assertive"
              className="p-3 rounded-md bg-error/10 border border-error/30 text-sm text-error"
            >
              Account temporarily locked. Try again in {state.lockoutMinutes} minutes.
            </div>
          )}

          {/* Generic Error */}
          {state.error && (
            <div
              role="alert"
              aria-live="assertive"
              className="p-3 rounded-md bg-error/10 border border-error/30 text-sm text-error"
            >
              {state.error}
            </div>
          )}

          <Input
            label="Email"
            type="email"
            required
            autoComplete="email"
            value={state.email}
            onChange={(e) => setState((prev) => ({ ...prev, email: e.target.value }))}
            disabled={state.loading || state.lockoutMinutes !== null}
          />

          <Input
            label="Password"
            type="password"
            required
            autoComplete="current-password"
            value={state.password}
            onChange={(e) => setState((prev) => ({ ...prev, password: e.target.value }))}
            disabled={state.loading || state.lockoutMinutes !== null}
          />

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full"
            loading={state.loading}
            disabled={state.lockoutMinutes !== null}
          >
            Sign In
          </Button>
        </form>

        {/* Links */}
        <div className="flex items-center justify-between text-sm">
          <a
            href="/forgot-password"
            className="text-primary hover:text-primary/80 transition-colors duration-fast focus-visible:ring-2 focus-visible:ring-primary outline-none rounded-sm"
          >
            Forgot password?
          </a>
          <a
            href="/signup"
            className="text-primary hover:text-primary/80 transition-colors duration-fast focus-visible:ring-2 focus-visible:ring-primary outline-none rounded-sm"
          >
            Create an account
          </a>
        </div>
      </div>
    </div>
  );
}

export default LoginPage;
