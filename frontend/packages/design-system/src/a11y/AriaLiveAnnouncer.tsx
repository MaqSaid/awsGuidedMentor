import { createContext, useContext, useCallback, useState, useRef, type ReactNode } from 'react';

interface AnnouncerContextValue {
  /** Announce a message politely (waits for current speech to finish). */
  announce: (message: string) => void;
  /** Announce a message assertively (interrupts current speech). */
  announceAssertive: (message: string) => void;
}

const AnnouncerContext = createContext<AnnouncerContextValue | null>(null);

const CLEAR_DELAY_MS = 5000;

const srOnlyStyle: React.CSSProperties = {
  position: 'absolute',
  width: '1px',
  height: '1px',
  padding: 0,
  margin: '-1px',
  overflow: 'hidden',
  clip: 'rect(0, 0, 0, 0)',
  whiteSpace: 'nowrap',
  borderWidth: 0,
};

export interface AriaLiveAnnouncerProps {
  children: ReactNode;
}

/**
 * AriaLiveAnnouncer — Provider component that renders hidden aria-live regions
 * and exposes the announce functions via context.
 *
 * Place this high in the component tree (e.g., wrapping your App).
 */
export function AriaLiveAnnouncer({ children }: AriaLiveAnnouncerProps) {
  const [politeMessage, setPoliteMessage] = useState('');
  const [assertiveMessage, setAssertiveMessage] = useState('');
  const politeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const assertiveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const announce = useCallback((message: string) => {
    // Clear and re-set to force screen reader re-announcement of identical messages
    setPoliteMessage('');
    requestAnimationFrame(() => {
      setPoliteMessage(message);
    });

    if (politeTimerRef.current) {
      clearTimeout(politeTimerRef.current);
    }
    politeTimerRef.current = setTimeout(() => {
      setPoliteMessage('');
    }, CLEAR_DELAY_MS);
  }, []);

  const announceAssertive = useCallback((message: string) => {
    setAssertiveMessage('');
    requestAnimationFrame(() => {
      setAssertiveMessage(message);
    });

    if (assertiveTimerRef.current) {
      clearTimeout(assertiveTimerRef.current);
    }
    assertiveTimerRef.current = setTimeout(() => {
      setAssertiveMessage('');
    }, CLEAR_DELAY_MS);
  }, []);

  return (
    <AnnouncerContext.Provider value={{ announce, announceAssertive }}>
      {children}
      {/* Polite live region — waits for current speech to finish */}
      <div aria-live="polite" aria-atomic="true" role="status" style={srOnlyStyle}>
        {politeMessage}
      </div>
      {/* Assertive live region — interrupts current speech */}
      <div aria-live="assertive" aria-atomic="true" role="alert" style={srOnlyStyle}>
        {assertiveMessage}
      </div>
    </AnnouncerContext.Provider>
  );
}

/**
 * useAnnounce — Hook to access aria-live announcement functions.
 * Used for: form validation errors, notification count changes, loading state changes.
 *
 * @throws Error if used outside of AriaLiveAnnouncer provider.
 */
export function useAnnounce(): AnnouncerContextValue {
  const context = useContext(AnnouncerContext);
  if (!context) {
    throw new Error('useAnnounce must be used within an AriaLiveAnnouncer provider');
  }
  return context;
}
