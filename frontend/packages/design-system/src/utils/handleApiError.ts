import type { ApiErrorResponse } from '../types/api-error';

/**
 * Result types for the error handler, discriminated by status-code routing.
 */
export type ApiErrorResult =
  | { type: 'validation'; fieldErrors: Record<string, string> }
  | { type: 'unauthorized'; action: 'refresh' | 'redirect' }
  | { type: 'conflict'; message: string }
  | { type: 'rate-limited'; retryAfterSeconds: number | null; message: string }
  | { type: 'server-error'; message: string; canRetry: boolean };

export interface HandleApiErrorOptions {
  /** Callback invoked when a retry action is triggered by the user */
  onRetry?: () => void;
  /** Callback to attempt token refresh. Return true if refresh succeeded. */
  onTokenRefresh?: () => Promise<boolean>;
  /** Callback to redirect to login page */
  onRedirectToLogin?: () => void;
  /** Callback to show a toast notification */
  onShowToast?: (message: string, options?: { retryAfterSeconds?: number; onRetry?: () => void }) => void;
}

/**
 * Routes API errors to the appropriate UI handling strategy based on HTTP status code.
 *
 * - 400 (Validation) → returns field errors for inline display
 * - 401 (Unauthorized) → attempts token refresh, redirects to /login on failure
 * - 409 (Conflict) → shows "temporarily unavailable" toast
 * - 429 (Rate Limited) → shows countdown toast with Retry-After
 * - 5xx (Server Error) → shows "Something went wrong" toast with retry button
 */
export async function handleApiError(
  error: ApiErrorResponse,
  options?: HandleApiErrorOptions
): Promise<ApiErrorResult> {
  const { onRetry, onTokenRefresh, onRedirectToLogin, onShowToast } = options ?? {};

  switch (error.statusCode) {
    case 400: {
      return {
        type: 'validation',
        fieldErrors: error.fieldErrors ?? {},
      };
    }

    case 401: {
      if (onTokenRefresh) {
        const refreshed = await onTokenRefresh();
        if (refreshed) {
          return { type: 'unauthorized', action: 'refresh' };
        }
      }
      onRedirectToLogin?.();
      return { type: 'unauthorized', action: 'redirect' };
    }

    case 409: {
      const message = error.message || 'This resource is temporarily unavailable. Please try again.';
      onShowToast?.(message);
      return { type: 'conflict', message };
    }

    case 429: {
      const retryAfterSeconds = error.fieldErrors?.['retryAfterSeconds']
        ? parseInt(error.fieldErrors['retryAfterSeconds'], 10)
        : null;
      const message = error.message || 'Too many requests. Please wait before trying again.';
      onShowToast?.(message, { retryAfterSeconds: retryAfterSeconds ?? undefined });
      return { type: 'rate-limited', retryAfterSeconds, message };
    }

    default: {
      // 5xx and any other unexpected status codes
      const message = 'Something went wrong. Please try again.';
      onShowToast?.(message, { onRetry });
      return { type: 'server-error', message, canRetry: true };
    }
  }
}
