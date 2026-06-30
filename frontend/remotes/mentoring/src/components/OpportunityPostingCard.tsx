/**
 * OpportunityPostingCard — displays opportunity details:
 * title, type badge, organisation, location, date (events), skills tags, days remaining.
 *
 * Requirements: 28.6, 28.7
 */
import type { OpportunityPosting } from '../types';

interface OpportunityPostingCardProps {
  posting: OpportunityPosting;
  className?: string;
}

const TYPE_BADGE_COLORS: Record<string, string> = {
  job: 'bg-secondary/10 text-secondary border-secondary/30',
  workshop: 'bg-accent/10 text-accent border-accent/30',
  event: 'bg-primary/10 text-primary border-primary/30',
  training: 'bg-success/10 text-success border-success/30',
};

const ACTION_LABELS: Record<string, string> = {
  job: 'Apply',
  workshop: 'Register',
  event: 'Register',
  training: 'Register',
};

export function OpportunityPostingCard({ posting, className = '' }: OpportunityPostingCardProps) {
  const typeBadgeClass = TYPE_BADGE_COLORS[posting.type] || TYPE_BADGE_COLORS.job;
  const actionLabel = ACTION_LABELS[posting.type] || 'View';

  const formattedDate = posting.eventDateTime
    ? new Date(posting.eventDateTime).toLocaleDateString('en-AU', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      })
    : null;

  return (
    <article
      className={[
        'glass-card p-4 rounded-lg transition-all duration-base',
        'hover:shadow-glow-orange hover:border-primary/20',
        className,
      ].join(' ')}
      aria-label={`${posting.type} opportunity: ${posting.title} at ${posting.organisationName}`}
    >
      {/* Header: Type badge + Days remaining */}
      <div className="flex items-start justify-between mb-2">
        <span
          className={[
            'inline-flex items-center text-xs px-2 py-0.5 rounded-full border font-medium capitalize',
            typeBadgeClass,
          ].join(' ')}
        >
          {posting.type}
        </span>
        <span className="text-xs text-text-muted">
          {posting.daysRemaining > 0
            ? `${posting.daysRemaining} day${posting.daysRemaining > 1 ? 's' : ''} left`
            : 'Expires today'}
        </span>
      </div>

      {/* Title */}
      <h3 className="text-text-primary font-semibold text-sm mb-1 line-clamp-2">
        {posting.title}
      </h3>

      {/* Organisation + Location */}
      <div className="flex items-center gap-2 text-xs text-text-secondary mb-2">
        <span>{posting.organisationName}</span>
        <span aria-hidden="true">·</span>
        <span>{posting.location}</span>
      </div>

      {/* Date for events */}
      {formattedDate && (
        <p className="text-xs text-text-muted mb-2">
          <time dateTime={posting.eventDateTime}>{formattedDate}</time>
        </p>
      )}

      {/* Mentor match badge */}
      {posting.isMentorMatch && (
        <span className="inline-flex items-center text-xs px-2 py-0.5 rounded-full bg-accent/10 text-accent border border-accent/20 mb-2">
          From your mentor
        </span>
      )}

      {/* Skills tags */}
      {posting.requiredSkills.length > 0 && (
        <div className="flex flex-wrap gap-1 mb-3" aria-label="Required skills">
          {posting.requiredSkills.slice(0, 4).map((skill) => (
            <span
              key={skill}
              className="text-xs px-2 py-0.5 rounded-full bg-surface-elevated text-text-muted border border-white/10"
            >
              {skill}
            </span>
          ))}
          {posting.requiredSkills.length > 4 && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-surface-elevated text-text-muted">
              +{posting.requiredSkills.length - 4}
            </span>
          )}
        </div>
      )}

      {/* Action button */}
      <a
        href={posting.externalUrl}
        target="_blank"
        rel="noopener noreferrer"
        className={[
          'inline-flex items-center gap-1 px-3 py-1.5 rounded-md text-sm font-medium',
          'bg-primary text-[#0a1628] hover:opacity-90 transition-opacity duration-fast',
          'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none',
        ].join(' ')}
        aria-label={`${actionLabel} for ${posting.title}`}
      >
        {actionLabel}
        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
        </svg>
      </a>
    </article>
  );
}
