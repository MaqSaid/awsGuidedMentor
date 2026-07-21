import { Link } from 'react-router-dom';

interface EmptyStateProps {
  title: string;
  description: string;
  ctaLabel?: string;
  ctaTo?: string;
  onCtaClick?: () => void;
  icon?: string;
}

/**
 * EmptyState — displayed when a page or section has no content.
 * Provides clear instructions and prominent call-to-action button.
 * Requirements: 25.5
 */
export function EmptyState({
  title,
  description,
  ctaLabel,
  ctaTo,
  onCtaClick,
  icon = '📭',
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-6 text-center">
      <span className="text-5xl mb-4" aria-hidden="true">{icon}</span>
      <h3
        className="text-xl font-bold text-text-primary mb-2"
        style={{ fontFamily: 'Outfit, sans-serif' }}
      >
        {title}
      </h3>
      <p className="text-sm text-text-secondary max-w-md mb-6">{description}</p>
      {ctaLabel && ctaTo && (
        <Link to={ctaTo} className="btn-violet text-sm">
          {ctaLabel}
        </Link>
      )}
      {ctaLabel && onCtaClick && !ctaTo && (
        <button onClick={onCtaClick} className="btn-violet text-sm">
          {ctaLabel}
        </button>
      )}
    </div>
  );
}
