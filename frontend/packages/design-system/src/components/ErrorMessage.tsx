import type { ReactNode } from 'react';
import { Button } from './Button';

/**
 * ErrorMessage — Displays friendly, non-technical error messages with retry button
 * and optional "Learn more" link. Includes visual spinner during retry.
 *
 * Requirements: 25.9, 25.10
 */

export interface ErrorMessageProps {
  /** Friendly error message to display */
  message?: string;
  /** Whether a retry operation is currently in progress */
  isRetrying?: boolean;
  /** Callback invoked when user clicks "Try Again" */
  onRetry?: () => void;
  /** Optional "Learn more" URL */
  learnMoreHref?: string;
  /** Icon to display (defaults to warning icon) */
  icon?: ReactNode;
  /** Additional className */
  className?: string;
  /** Whether to display inline (compact) vs block (full card) */
  variant?: 'inline' | 'block';
}

export function ErrorMessage({
  message = 'Something went wrong. Please try again.',
  isRetrying = false,
  onRetry,
  learnMoreHref,
  icon,
  className = '',
  variant = 'block',
}: ErrorMessageProps) {
  const defaultIcon = (
    <svg
      className="h-8 w-8 text-error"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
      strokeWidth={1.5}
      stroke="currentColor"
      aria-hidden="true"
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
      />
    </svg>
  );

  if (variant === 'inline') {
    return (
      <div
        className={`flex items-center gap-3 p-3 rounded-md bg-error/10 border border-error/20 ${className}`}
        role="alert"
      >
        <div className="flex-shrink-0">
          {icon || (
            <svg
              className="h-4 w-4 text-error"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          )}
        </div>
        <p className="text-sm text-error flex-1">{message}</p>
        {onRetry && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onRetry}
            loading={isRetrying}
            className="text-error hover:text-error"
          >
            Retry
          </Button>
        )}
      </div>
    );
  }

  return (
    <div
      className={`glass-card p-6 rounded-lg text-center flex flex-col items-center gap-4 ${className}`}
      role="alert"
    >
      {/* Icon */}
      <div className="flex-shrink-0">{icon || defaultIcon}</div>

      {/* Message */}
      <p className="text-sm text-text-secondary max-w-sm">{message}</p>

      {/* Actions */}
      <div className="flex items-center gap-3">
        {onRetry && (
          <Button variant="primary" size="md" onClick={onRetry} loading={isRetrying}>
            {isRetrying ? 'Retrying...' : 'Try Again'}
          </Button>
        )}
        {learnMoreHref && (
          <a
            href={learnMoreHref}
            className="text-sm text-primary hover:underline"
            target="_blank"
            rel="noopener noreferrer"
          >
            Learn more
          </a>
        )}
      </div>
    </div>
  );
}
