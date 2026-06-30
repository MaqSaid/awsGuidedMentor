import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * useViewTransition — wraps navigation with the View Transitions API
 * for smooth cross-page animations. Falls back gracefully in unsupported browsers.
 */
export function useViewTransition() {
  const navigate = useNavigate();

  const transitionTo = useCallback((path: string) => {
    if (document.startViewTransition) {
      document.startViewTransition(() => {
        navigate(path);
      });
    } else {
      navigate(path);
    }
  }, [navigate]);

  return transitionTo;
}
