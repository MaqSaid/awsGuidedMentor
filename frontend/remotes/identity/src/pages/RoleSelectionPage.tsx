import { useState, useCallback } from 'react';
import { Button } from '@guided-mentor/design-system';

/**
 * RoleSelectionPage — Two large cards for Mentor/Mentee selection.
 * Blocks all navigation until a role is selected.
 * AWS Purple gradient border for Mentor, AWS Teal for Mentee.
 *
 * Requirements: 2.1, 2.3, 13.5
 */

type RoleChoice = 'mentor' | 'mentee' | null;

export function RoleSelectionPage() {
  const [selectedRole, setSelectedRole] = useState<RoleChoice>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleConfirm = useCallback(async () => {
    if (!selectedRole) return;
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/v1/users/role', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role: selectedRole }),
      });

      if (!response.ok) {
        setError('Unable to set role. Please try again.');
        setLoading(false);
        return;
      }

      // Redirect to onboarding
      window.location.href = `/onboarding?role=${selectedRole}`;
    } catch {
      setError('Unable to connect. Please try again.');
      setLoading(false);
    }
  }, [selectedRole]);

  return (
    <div
      className="flex items-center justify-center min-h-screen px-4"
      data-testid="identity-role-selection-page"
    >
      <div className="w-full max-w-2xl space-y-8">
        {/* Header */}
        <div className="text-center space-y-2">
          <h1 className="text-3xl font-bold text-text-primary">Choose Your Role</h1>
          <p className="text-text-secondary">
            Select how you'd like to participate in the GuidedMentor community.
            <br />
            <span className="text-xs text-text-muted">You can add the other role later.</span>
          </p>
        </div>

        {error && (
          <div
            role="alert"
            aria-live="assertive"
            className="p-3 rounded-md bg-error/10 border border-error/30 text-sm text-error text-center"
          >
            {error}
          </div>
        )}

        {/* Role Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6" role="radiogroup" aria-label="Role selection">
          {/* Mentor Card */}
          <button
            type="button"
            role="radio"
            aria-checked={selectedRole === 'mentor'}
            onClick={() => setSelectedRole('mentor')}
            className={[
              'relative p-6 rounded-md text-left transition-all duration-base cursor-pointer',
              'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none',
              'gradient-border-mentor',
              selectedRole === 'mentor'
                ? 'glass-card shadow-glow-purple scale-[1.02]'
                : 'glass-card hover:scale-[1.01]',
            ].join(' ')}
            style={selectedRole === 'mentor' ? { boxShadow: '0 0 24px rgba(140, 79, 255, 0.25)' } : undefined}
          >
            {/* Selected Indicator */}
            {selectedRole === 'mentor' && (
              <div className="absolute top-3 right-3">
                <svg className="h-6 w-6 text-[#8C4FFF]" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
              </div>
            )}

            {/* Icon */}
            <div className="h-14 w-14 rounded-lg bg-[#8C4FFF]/10 flex items-center justify-center mb-4">
              <svg className="h-7 w-7 text-[#8C4FFF]" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
              </svg>
            </div>

            <h2 className="text-xl font-semibold text-text-primary mb-2">
              I want to be a Mentor
            </h2>
            <p className="text-sm text-text-secondary">
              Share your AWS expertise and help community members grow their careers.
              Guide mentees through personalised 35-minute sessions.
            </p>
          </button>

          {/* Mentee Card */}
          <button
            type="button"
            role="radio"
            aria-checked={selectedRole === 'mentee'}
            onClick={() => setSelectedRole('mentee')}
            className={[
              'relative p-6 rounded-md text-left transition-all duration-base cursor-pointer',
              'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none',
              'gradient-border-mentee',
              selectedRole === 'mentee'
                ? 'glass-card shadow-glow-teal scale-[1.02]'
                : 'glass-card hover:scale-[1.01]',
            ].join(' ')}
            style={selectedRole === 'mentee' ? { boxShadow: '0 0 24px rgba(0, 163, 161, 0.25)' } : undefined}
          >
            {selectedRole === 'mentee' && (
              <div className="absolute top-3 right-3">
                <svg className="h-6 w-6 text-secondary" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
              </div>
            )}

            <div className="h-14 w-14 rounded-lg bg-secondary/10 flex items-center justify-center mb-4">
              <svg className="h-7 w-7 text-secondary" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>
            </div>

            <h2 className="text-xl font-semibold text-text-primary mb-2">
              I want to find a Mentor (Mentee)
            </h2>
            <p className="text-sm text-text-secondary">
              Get matched with experienced AWS professionals based on your goals,
              skills, and learning preferences.
            </p>
          </button>
        </div>

        {/* Confirm button */}
        <div className="flex justify-center">
          <Button
            variant="primary"
            size="lg"
            onClick={handleConfirm}
            disabled={!selectedRole}
            loading={loading}
            className="min-w-[200px]"
          >
            Continue as {selectedRole === 'mentor' ? 'Mentor' : selectedRole === 'mentee' ? 'Mentee' : '...'}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default RoleSelectionPage;
