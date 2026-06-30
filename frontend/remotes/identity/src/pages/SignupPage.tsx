import { useState, useCallback, useRef, useEffect, type FormEvent } from 'react';
import { Button, Input } from '@guided-mentor/design-system';

/**
 * SignupPage — Email + password form with inline validation (300ms debounce),
 * visual password strength indicator, and EmailVerificationStep.
 *
 * Requirements: 1.1, 1.2, 1.5, 13.1, 13.2, 13.4, 25.7
 */

// Password validation rules
const PASSWORD_RULES = [
  { id: 'length', label: '12+ characters', test: (p: string) => p.length >= 12 },
  { id: 'upper', label: 'Uppercase letter', test: (p: string) => /[A-Z]/.test(p) },
  { id: 'lower', label: 'Lowercase letter', test: (p: string) => /[a-z]/.test(p) },
  { id: 'digit', label: 'Number', test: (p: string) => /\d/.test(p) },
  { id: 'special', label: 'Special character', test: (p: string) => /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?`~]/.test(p) },
] as const;

function getPasswordStrength(password: string): { score: number; label: string; color: string } {
  if (!password) return { score: 0, label: '', color: '' };
  const passed = PASSWORD_RULES.filter((r) => r.test(password)).length;
  if (passed <= 1) return { score: 20, label: 'Weak', color: 'bg-error' };
  if (passed <= 2) return { score: 40, label: 'Fair', color: 'bg-warning' };
  if (passed <= 3) return { score: 60, label: 'Good', color: 'bg-warning' };
  if (passed <= 4) return { score: 80, label: 'Strong', color: 'bg-success/70' };
  return { score: 100, label: 'Very Strong', color: 'bg-success' };
}

interface SignupState {
  email: string;
  password: string;
  confirmPassword: string;
  loading: boolean;
  error: string | null;
  passwordValidation: Record<string, boolean>;
  showVerification: boolean;
}

export function SignupPage() {
  const [state, setState] = useState<SignupState>({
    email: '',
    password: '',
    confirmPassword: '',
    loading: false,
    error: null,
    passwordValidation: {},
    showVerification: false,
  });

  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [debouncedPassword, setDebouncedPassword] = useState('');
  const announceRef = useRef<HTMLDivElement>(null);

  // Debounce password validation (300ms)
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setDebouncedPassword(state.password);
      if (state.password) {
        const validation: Record<string, boolean> = {};
        PASSWORD_RULES.forEach((rule) => {
          validation[rule.id] = rule.test(state.password);
        });
        setState((prev) => ({ ...prev, passwordValidation: validation }));

        // Announce validation errors for screen readers
        const failures = PASSWORD_RULES.filter((r) => !r.test(state.password));
        if (failures.length > 0 && announceRef.current) {
          announceRef.current.textContent = `Password requirements not met: ${failures.map((f) => f.label).join(', ')}`;
        }
      }
    }, 300);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [state.password]);

  const handleSubmit = useCallback(
    async (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      setState((prev) => ({ ...prev, loading: true, error: null }));

      // Validate all password rules pass
      const allRulesPass = PASSWORD_RULES.every((r) => r.test(state.password));
      if (!allRulesPass) {
        setState((prev) => ({
          ...prev,
          loading: false,
          error: 'Please meet all password requirements.',
        }));
        return;
      }

      if (state.password !== state.confirmPassword) {
        setState((prev) => ({
          ...prev,
          loading: false,
          error: 'Passwords do not match.',
        }));
        return;
      }

      try {
        const response = await fetch('/v1/auth/signup/email', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email: state.email, password: state.password }),
        });

        if (!response.ok) {
          const data = (await response.json()) as { message?: string };
          setState((prev) => ({
            ...prev,
            loading: false,
            error: data.message ?? 'Signup failed. Please try again.',
          }));
          return;
        }

        // Success → show verification step
        setState((prev) => ({ ...prev, loading: false, showVerification: true }));
      } catch {
        setState((prev) => ({
          ...prev,
          loading: false,
          error: 'Unable to connect. Please try again.',
        }));
      }
    },
    [state.email, state.password, state.confirmPassword]
  );

  if (state.showVerification) {
    return <EmailVerificationStep email={state.email} />;
  }

  const strength = getPasswordStrength(debouncedPassword);

  return (
    <div
      className="flex items-center justify-center min-h-screen px-4"
      data-testid="identity-signup-page"
    >
      <div className="glass-card p-8 w-full max-w-md space-y-6">
        {/* Header */}
        <div className="text-center space-y-2">
          <h1 className="text-2xl font-bold text-text-primary">Create Account</h1>
          <p className="text-sm text-text-secondary">
            Join the AWS GuidedMentor community
          </p>
        </div>

        {/* Google OAuth */}
        <Button
          variant="secondary"
          size="lg"
          className="w-full gap-3"
          onClick={() => {
            const cognitoDomain = import.meta.env.VITE_COGNITO_DOMAIN ?? '';
            const clientId = import.meta.env.VITE_COGNITO_CLIENT_ID ?? '';
            const redirectUri = import.meta.env.VITE_OAUTH_REDIRECT_URI ?? window.location.origin + '/auth/callback';
            window.location.href = `${cognitoDomain}/oauth2/authorize?identity_provider=Google&response_type=code&client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&scope=openid+email+profile`;
          }}
          aria-label="Sign up with Google"
        >
          <svg className="h-5 w-5" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
          </svg>
          Sign up with Google
        </Button>

        {/* Divider */}
        <div className="flex items-center gap-4">
          <div className="flex-1 h-px bg-[rgba(255,255,255,0.08)]" />
          <span className="text-xs text-text-muted uppercase tracking-wide">or</span>
          <div className="flex-1 h-px bg-[rgba(255,255,255,0.08)]" />
        </div>

        {/* Signup Form */}
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
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
            disabled={state.loading}
          />

          <div className="space-y-2">
            <Input
              label="Password"
              type="password"
              required
              autoComplete="new-password"
              value={state.password}
              onChange={(e) => setState((prev) => ({ ...prev, password: e.target.value }))}
              disabled={state.loading}
              aria-describedby="password-strength password-rules"
            />

            {/* Password Strength Indicator */}
            {debouncedPassword && (
              <div className="space-y-2" id="password-strength">
                <div className="flex items-center gap-2">
                  <div className="flex-1 h-1.5 rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all duration-base ${strength.color}`}
                      style={{ width: `${strength.score}%` }}
                    />
                  </div>
                  <span className="text-xs text-text-muted">{strength.label}</span>
                </div>

                {/* Individual rule checks */}
                <ul
                  id="password-rules"
                  className="grid grid-cols-2 gap-1 text-xs"
                  aria-label="Password requirements"
                >
                  {PASSWORD_RULES.map((rule) => {
                    const passed = state.passwordValidation[rule.id];
                    return (
                      <li key={rule.id} className="flex items-center gap-1.5">
                        {passed ? (
                          <svg className="h-3 w-3 text-success flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                            <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                          </svg>
                        ) : (
                          <svg className="h-3 w-3 text-text-muted flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                            <circle cx="10" cy="10" r="4" />
                          </svg>
                        )}
                        <span className={passed ? 'text-success' : 'text-text-muted'}>
                          {rule.label}
                        </span>
                      </li>
                    );
                  })}
                </ul>
              </div>
            )}
          </div>

          <Input
            label="Confirm Password"
            type="password"
            required
            autoComplete="new-password"
            value={state.confirmPassword}
            onChange={(e) => setState((prev) => ({ ...prev, confirmPassword: e.target.value }))}
            disabled={state.loading}
            error={
              state.confirmPassword && state.password !== state.confirmPassword
                ? 'Passwords do not match'
                : undefined
            }
          />

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full"
            loading={state.loading}
          >
            Create Account
          </Button>
        </form>

        {/* Link to login */}
        <p className="text-center text-sm text-text-secondary">
          Already have an account?{' '}
          <a
            href="/login"
            className="text-primary hover:text-primary/80 transition-colors duration-fast focus-visible:ring-2 focus-visible:ring-primary outline-none rounded-sm"
          >
            Sign in
          </a>
        </p>
      </div>

      {/* SR-only live region for password validation announcements */}
      <div
        ref={announceRef}
        aria-live="polite"
        aria-atomic="true"
        className="sr-only"
        style={{ position: 'absolute', width: '1px', height: '1px', overflow: 'hidden', clip: 'rect(0,0,0,0)' }}
      />
    </div>
  );
}

/**
 * EmailVerificationStep — 6-digit code input with resend button and 5-attempt limit.
 */
function EmailVerificationStep({ email }: { email: string }) {
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [attempts, setAttempts] = useState(0);
  const [resendCooldown, setResendCooldown] = useState(0);
  const maxAttempts = 5;

  // Resend cooldown timer
  useEffect(() => {
    if (resendCooldown <= 0) return;
    const timer = setInterval(() => {
      setResendCooldown((c) => c - 1);
    }, 1000);
    return () => clearInterval(timer);
  }, [resendCooldown]);

  const handleVerify = useCallback(
    async (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      if (attempts >= maxAttempts) {
        setError('Maximum attempts reached. Please request a new code.');
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const response = await fetch('/v1/auth/verify-email', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, code }),
        });

        if (!response.ok) {
          setAttempts((a) => a + 1);
          setError('Invalid code. Please try again.');
          setLoading(false);
          return;
        }

        // Success → redirect to role selection
        window.location.href = '/role-selection';
      } catch {
        setError('Unable to connect. Please try again.');
        setLoading(false);
      }
    },
    [email, code, attempts]
  );

  const handleResend = useCallback(async () => {
    setResendCooldown(60);
    setError(null);
    try {
      await fetch('/v1/auth/signup/email', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, resend: true }),
      });
    } catch {
      setError('Unable to resend code.');
    }
  }, [email]);

  return (
    <div className="flex items-center justify-center min-h-screen px-4" data-testid="identity-signup-page">
      <div className="glass-card p-8 w-full max-w-md space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-2xl font-bold text-text-primary">Verify Your Email</h1>
          <p className="text-sm text-text-secondary">
            We sent a 6-digit code to <strong className="text-text-primary">{email}</strong>
          </p>
        </div>

        <form onSubmit={handleVerify} className="space-y-4" noValidate>
          {error && (
            <div
              role="alert"
              aria-live="assertive"
              className="p-3 rounded-md bg-error/10 border border-error/30 text-sm text-error"
            >
              {error}
            </div>
          )}

          {attempts >= maxAttempts && (
            <div
              role="alert"
              aria-live="assertive"
              className="p-3 rounded-md bg-warning/10 border border-warning/30 text-sm text-warning"
            >
              Maximum verification attempts reached. Please request a new code.
            </div>
          )}

          <Input
            label="Verification Code"
            type="text"
            required
            inputMode="numeric"
            maxLength={6}
            value={code}
            onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
            disabled={loading || attempts >= maxAttempts}
            helpText={`Attempt ${attempts + 1} of ${maxAttempts}`}
            autoComplete="one-time-code"
          />

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full"
            loading={loading}
            disabled={code.length !== 6 || attempts >= maxAttempts}
          >
            Verify Email
          </Button>
        </form>

        <div className="text-center">
          <button
            type="button"
            onClick={handleResend}
            disabled={resendCooldown > 0}
            className="text-sm text-primary hover:text-primary/80 transition-colors duration-fast disabled:text-text-muted disabled:cursor-not-allowed focus-visible:ring-2 focus-visible:ring-primary outline-none rounded-sm"
          >
            {resendCooldown > 0 ? `Resend code in ${resendCooldown}s` : 'Resend code'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default SignupPage;
