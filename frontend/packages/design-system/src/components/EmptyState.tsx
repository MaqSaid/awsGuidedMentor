import type { ReactNode } from 'react';
import { Button } from './Button';

/**
 * EmptyState — Standardized empty state component with clear instructions
 * and prominent call-to-action buttons for when a page/section has no content.
 *
 * Requirements: 25.5
 */

export interface EmptyStateProps {
  /** Icon to display above the message (SVG element or emoji) */
  icon?: ReactNode;
  /** Main heading/title for the empty state */
  title?: string;
  /** Descriptive message explaining what the user can do */
  message: string;
  /** Primary CTA button label */
  actionLabel?: string;
  /** Primary CTA link href (renders as link-styled button) */
  actionHref?: string;
  /** Primary CTA click handler (alternative to href) */
  onAction?: () => void;
  /** Secondary CTA button label */
  secondaryLabel?: string;
  /** Secondary CTA click handler */
  onSecondaryAction?: () => void;
  /** Additional classNames */
  className?: string;
}

export function EmptyState({
  icon,
  title,
  message,
  actionLabel,
  actionHref,
  onAction,
  secondaryLabel,
  onSecondaryAction,
  className = '',
}: EmptyStateProps) {
  return (
    <div
      className={`glass-card p-8 rounded-lg text-center flex flex-col items-center gap-4 ${className}`}
      role="status"
      aria-label={title || 'Empty state'}
    >
      {/* Icon */}
      {icon && (
        <div className="text-4xl text-text-muted" aria-hidden="true">
          {icon}
        </div>
      )}

      {/* Title */}
      {title && (
        <h3 className="text-lg font-semibold text-text-primary">{title}</h3>
      )}

      {/* Message */}
      <p className="text-sm text-text-muted max-w-sm">{message}</p>

      {/* Actions */}
      {(actionLabel || secondaryLabel) && (
        <div className="flex items-center gap-3 mt-2">
          {actionLabel && actionHref && (
            <a
              href={actionHref}
              className="inline-flex items-center justify-center gap-2 px-4 py-2 bg-primary text-[#0a1628] font-semibold rounded-md hover:opacity-90 transition-opacity text-sm"
            >
              {actionLabel}
            </a>
          )}
          {actionLabel && onAction && !actionHref && (
            <Button variant="primary" size="md" onClick={onAction}>
              {actionLabel}
            </Button>
          )}
          {secondaryLabel && onSecondaryAction && (
            <Button variant="ghost" size="md" onClick={onSecondaryAction}>
              {secondaryLabel}
            </Button>
          )}
        </div>
      )}
    </div>
  );
}
