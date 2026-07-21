/**
 * TrackerProvider — React context providing the EventTracker instance and
 * typed tracking hooks (trackPageView, trackClick, trackFormStep, trackError, trackA11y).
 *
 * Integrates with RoleProvider to automatically tag events with activeRole.
 *
 * Requirements: 30.1, 30.2, 30.3, 30.11
 */
import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  type ReactNode,
} from 'react';
import { EventTracker, type ConsentStatus } from './EventTracker';

export interface TrackerContextValue {
  trackPageView: (pageName: string) => void;
  trackClick: (element: string, metadata?: Record<string, unknown>) => void;
  trackFormStep: (formName: string, step: number, duration: number) => void;
  trackError: (errorType: string, page: string) => void;
  trackA11y: (feature: string, metadata?: Record<string, unknown>) => void;
  setConsent: (status: ConsentStatus) => void;
  getConsent: () => ConsentStatus;
}

const TrackerContext = createContext<TrackerContextValue | null>(null);

interface TrackerProviderProps {
  children: ReactNode;
  /** The user's active role from the RoleProvider. */
  activeRole?: 'mentor' | 'mentee' | null;
  /** Optional endpoint override (for testing). */
  endpoint?: string;
}

export function TrackerProvider({
  children,
  activeRole,
  endpoint,
}: TrackerProviderProps) {
  const trackerRef = useRef<EventTracker | null>(null);

  // Lazily initialize the EventTracker
  if (trackerRef.current === null) {
    trackerRef.current = new EventTracker(endpoint);
  }

  const tracker = trackerRef.current;

  // Sync active role into tracker whenever it changes
  useEffect(() => {
    if (activeRole) {
      tracker.setActiveRole(activeRole);
    }
  }, [activeRole, tracker]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      tracker.destroy();
    };
  }, [tracker]);

  const value = useMemo<TrackerContextValue>(
    () => ({
      trackPageView: (pageName: string) =>
        tracker.track('page_view', { pageName }),
      trackClick: (element: string, metadata?: Record<string, unknown>) =>
        tracker.track('click', { element, ...metadata }),
      trackFormStep: (formName: string, step: number, duration: number) =>
        tracker.track('form_step', { formName, step, duration }),
      trackError: (errorType: string, page: string) =>
        tracker.track('error_encountered', { errorType, page }),
      trackA11y: (feature: string, metadata?: Record<string, unknown>) =>
        tracker.track('accessibility_feature', { feature, ...metadata }),
      setConsent: (status: ConsentStatus) => tracker.setConsent(status),
      getConsent: () => tracker.getConsent(),
    }),
    [tracker]
  );

  return (
    <TrackerContext value={value}>{children}</TrackerContext>
  );
}

/**
 * useTracker — Access the tracking functions from within TrackerProvider.
 * Throws if used outside the provider.
 */
export function useTracker(): TrackerContextValue {
  const context = useContext(TrackerContext);
  if (!context) {
    throw new Error('useTracker must be used within a TrackerProvider');
  }
  return context;
}

export { TrackerContext };
