import { useRef, useCallback } from 'react';
import { preload, preconnect } from 'react-dom';

/**
 * Module-level Set for deduplication — never preload the same URL twice per session.
 */
const preloadedUrls = new Set<string>();

/**
 * usePreloadRemote — preloads a federated remote entry script on navigation intent.
 * Fires after a 150ms debounce (mouseenter/focus), cancels on mouseleave/blur.
 * Calls `preconnect` only for cross-origin remotes.
 * Silently discards all errors to avoid disrupting the user experience.
 */
export function usePreloadRemote(remoteEntry: string) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const onIntent = useCallback(() => {
    timerRef.current = setTimeout(() => {
      if (!preloadedUrls.has(remoteEntry)) {
        try {
          preload(remoteEntry, { as: 'script' });
          const origin = new URL(remoteEntry).origin;
          if (origin !== window.location.origin) {
            preconnect(origin);
          }
          preloadedUrls.add(remoteEntry);
        } catch {
          /* silently discard */
        }
      }
    }, 150);
  }, [remoteEntry]);

  const onCancel = useCallback(() => {
    clearTimeout(timerRef.current);
  }, []);

  return {
    onMouseEnter: onIntent,
    onFocus: onIntent,
    onMouseLeave: onCancel,
    onBlur: onCancel,
  };
}
