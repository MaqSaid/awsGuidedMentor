/**
 * ConsentBanner — Displays a first-visit consent banner for tracking opt-in/out.
 * When opted out, only auth and error events are tracked.
 *
 * Requirements: 30.7, 30.8
 */
import { useState, useEffect } from 'react';
import { useTracker } from './TrackerProvider';
import type { ConsentStatus } from './EventTracker';

export function ConsentBanner() {
  const { setConsent, getConsent } = useTracker();
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    // Show banner only if consent has not been decided yet
    const currentConsent = getConsent();
    if (currentConsent === 'pending') {
      setVisible(true);
    }
  }, [getConsent]);

  const handleAccept = () => {
    setConsent('granted');
    setVisible(false);
  };

  const handleDecline = () => {
    setConsent('denied');
    setVisible(false);
  };

  const handleUpdateConsent = async (status: ConsentStatus) => {
    try {
      await fetch('/v1/analytics/consent', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ consent: status }),
      });
    } catch {
      // Silent fail — consent is persisted locally regardless
    }
  };

  if (!visible) return null;

  return (
    <div
      role="dialog"
      aria-label="Tracking consent"
      aria-describedby="consent-description"
      className="consent-banner"
      style={{
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        padding: '1rem 1.5rem',
        backgroundColor: 'rgba(15, 23, 42, 0.95)',
        backdropFilter: 'blur(12px)',
        borderTop: '1px solid rgba(148, 163, 184, 0.2)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: '1rem',
        zIndex: 9999,
      }}
    >
      <p id="consent-description" style={{ margin: 0, color: '#e2e8f0', fontSize: '0.875rem' }}>
        We collect anonymous usage data to improve your platform experience. No personally
        identifiable information is stored. You can opt out at any time.
      </p>
      <div style={{ display: 'flex', gap: '0.5rem', flexShrink: 0 }}>
        <button
          onClick={() => {
            handleDecline();
            void handleUpdateConsent('denied');
          }}
          aria-label="Decline tracking"
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '0.375rem',
            border: '1px solid rgba(148, 163, 184, 0.3)',
            backgroundColor: 'transparent',
            color: '#94a3b8',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
        >
          Opt out
        </button>
        <button
          onClick={() => {
            handleAccept();
            void handleUpdateConsent('granted');
          }}
          aria-label="Accept tracking"
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '0.375rem',
            border: 'none',
            backgroundColor: '#3b82f6',
            color: '#ffffff',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
        >
          Accept
        </button>
      </div>
    </div>
  );
}
