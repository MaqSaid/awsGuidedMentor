import { useState, useCallback, useRef } from 'react';

/**
 * useRetry — Retry mechanism with visual spinner for all failure states.
 * Provides loading state, error state, and retry function with exponential backoff.
 *
 * Requirements: 25.9, 25.10
 */

export interface UseRetryOptions {
  /** Maximum number of retry attempts (default: 3) */
  maxRetries?: number;
  /** Base delay in ms between retries with exponential backoff (default: 1000ms) */
  baseDelayMs?: number;
  /** Callback invoked on successful retry */
  onSuccess?: () => void;
  /** Callback invoked when all retries are exhausted */
  onExhausted?: () => void;
}

export interface UseRetryReturn {
  /** Whether a retry operation is currently in progress */
  isRetrying: boolean;
  /** Current retry attempt number (0 = not retrying) */
  attempt: number;
  /** Whether all retries have been exhausted */
  exhausted: boolean;
  /** Trigger a retry of the provided async operation */
  retry: (operation: () => Promise<void>) => Promise<void>;
  /** Reset the retry state */
  reset: () => void;
}

export function useRetry(options?: UseRetryOptions): UseRetryReturn {
  const { maxRetries = 3, baseDelayMs = 1000, onSuccess, onExhausted } = options ?? {};

  const [isRetrying, setIsRetrying] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [exhausted, setExhausted] = useState(false);
  const abortRef = useRef(false);

  const retry = useCallback(
    async (operation: () => Promise<void>) => {
      abortRef.current = false;
      setIsRetrying(true);
      setExhausted(false);

      let currentAttempt = 0;

      while (currentAttempt < maxRetries && !abortRef.current) {
        currentAttempt++;
        setAttempt(currentAttempt);

        try {
          await operation();
          // Success
          setIsRetrying(false);
          setAttempt(0);
          onSuccess?.();
          return;
        } catch {
          if (currentAttempt >= maxRetries) {
            break;
          }
          // Exponential backoff
          const delay = baseDelayMs * Math.pow(2, currentAttempt - 1);
          await new Promise((resolve) => setTimeout(resolve, delay));
        }
      }

      // All retries exhausted
      setIsRetrying(false);
      setExhausted(true);
      onExhausted?.();
    },
    [maxRetries, baseDelayMs, onSuccess, onExhausted]
  );

  const reset = useCallback(() => {
    abortRef.current = true;
    setIsRetrying(false);
    setAttempt(0);
    setExhausted(false);
  }, []);

  return { isRetrying, attempt, exhausted, retry, reset };
}
