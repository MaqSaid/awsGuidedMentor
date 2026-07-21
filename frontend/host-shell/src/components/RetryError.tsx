import { useState } from 'react';

interface RetryErrorProps {
  message?: string;
  onRetry: () => void | Promise<void>;
  learnMoreUrl?: string;
}

/**
 * RetryError — friendly, non-technical error display with retry button.
 * Shows a spinner during retry and optional "Learn more" link.
 * Requirements: 25.9, 25.10
 */
export function RetryError({
  message = 'Something went wrong. Please try again.',
  onRetry,
  learnMoreUrl,
}: RetryErrorProps) {
  const [isRetrying, setIsRetrying] = useState(false);

  async function handleRetry() {
    setIsRetrying(true);
    try {
      await onRetry();
    } finally {
      setIsRetrying(false);
    }
  }

  return (
    <div
      role="alert"
      aria-live="polite"
      className="flex flex-col items-center justify-center py-10 px-6 text-center"
    >
      <div className="w-12 h-12 rounded-full bg-rose/10 flex items-center justify-center mb-4">
        <span className="text-rose text-xl" aria-hidden="true">⚠</span>
      </div>
      <p className="text-sm text-text-secondary mb-4 max-w-md">{message}</p>
      <div className="flex items-center gap-3">
        <button
          onClick={handleRetry}
          disabled={isRetrying}
          className="btn-ghost text-sm px-4 py-2 flex items-center gap-2 disabled:opacity-50"
          aria-label="Retry"
        >
          {isRetrying && (
            <span className="w-4 h-4 border-2 border-violet border-t-transparent rounded-full animate-spin" aria-hidden="true" />
          )}
          {isRetrying ? 'Retrying...' : 'Try Again'}
        </button>
        {learnMoreUrl && (
          <a
            href={learnMoreUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="text-sm text-violet-light hover:text-violet transition-colors"
          >
            Learn more
          </a>
        )}
      </div>
    </div>
  );
}
