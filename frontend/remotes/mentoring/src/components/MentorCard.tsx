/**
 * MentorCard — displays mentor info with score badge, expertise tags,
 * availability summary, locked overlay for unavailable mentors.
 *
 * Requirements: 5.9, 6.1, 6.7, 28.5, 32.3, 32.9
 */
import { forwardRef } from 'react';
import type { MentorBrowseResult } from '../types';
import { CompatibilityBadge } from './CompatibilityBadge';
import { SharingOpportunitiesBadge } from './SharingOpportunitiesBadge';
import { OnBreakBadge } from './OnBreakBadge';

interface MentorCardProps {
  mentor: MentorBrowseResult;
  isLocked?: boolean;
  onSelect?: (mentorId: string) => void;
  className?: string;
}

const MAX_VISIBLE_TAGS = 5;

export const MentorCard = forwardRef<HTMLDivElement, MentorCardProps>(
  ({ mentor, isLocked = false, onSelect, className = '' }, ref) => {
    const isUnavailable = mentor.availabilityStatus === 'unavailable';
    const visibleExpertise = mentor.expertiseAreas.slice(0, MAX_VISIBLE_TAGS);
    const overflowCount = mentor.expertiseAreas.length - MAX_VISIBLE_TAGS;

    const availableDays = Object.keys(mentor.availability || {});
    const availabilitySummary = availableDays.length > 0
      ? `${availableDays.length} day${availableDays.length > 1 ? 's' : ''}/week`
      : 'Schedule not set';

    const handleKeyDown = (e: React.KeyboardEvent) => {
      if ((e.key === 'Enter' || e.key === ' ') && onSelect && !isLocked) {
        e.preventDefault();
        onSelect(mentor.mentorId);
      }
    };

    return (
      <div
        ref={ref}
        role="button"
        tabIndex={0}
        aria-label={`${mentor.displayName}, ${mentor.professionalTitle} at ${mentor.companyName}. ${mentor.compatibilityScore}% match.${isLocked ? ' Currently unavailable.' : ''}`}
        aria-disabled={isLocked}
        onClick={() => !isLocked && onSelect?.(mentor.mentorId)}
        onKeyDown={handleKeyDown}
        className={[
          'relative glass-card p-4 rounded-lg transition-all duration-base',
          'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
          'outline-none cursor-pointer',
          isLocked
            ? 'opacity-60 cursor-not-allowed'
            : 'hover:shadow-glow-orange hover:border-primary/30',
          className,
        ].join(' ')}
      >
        {/* Locked overlay */}
        {isLocked && (
          <div
            className="absolute inset-0 rounded-lg bg-background/50 backdrop-blur-sm flex items-center justify-center z-10"
            aria-hidden="true"
          >
            <span className="text-text-muted text-sm font-medium px-3 py-1 bg-surface rounded-full border border-white/10">
              Temporarily Unavailable
            </span>
          </div>
        )}

        {/* Header: Photo + Name + Score */}
        <div className="flex items-start gap-3 mb-3">
          <div className="w-12 h-12 rounded-full bg-surface-elevated border border-white/10 flex items-center justify-center shrink-0 overflow-hidden">
            {mentor.profilePhotoUrl ? (
              <img
                src={mentor.profilePhotoUrl}
                alt=""
                className="w-full h-full object-cover"
              />
            ) : (
              <span className="text-lg font-semibold text-text-secondary">
                {mentor.displayName.charAt(0)}
              </span>
            )}
          </div>
          <div className="flex-1 min-w-0">
            <h3 className="text-text-primary font-semibold text-sm truncate">
              {mentor.displayName}
            </h3>
            <p className="text-text-muted text-xs truncate">
              {mentor.professionalTitle} · {mentor.companyName}
            </p>
            <p className="text-text-muted text-xs">{mentor.awsChapter}</p>
          </div>
          <CompatibilityBadge score={mentor.compatibilityScore} size="sm" />
        </div>

        {/* Expertise tags */}
        <div className="flex flex-wrap gap-1 mb-3" aria-label="Expertise areas">
          {visibleExpertise.map((skill) => (
            <span
              key={skill}
              className="text-xs px-2 py-0.5 rounded-full bg-accent/10 text-accent border border-accent/20"
            >
              {skill}
            </span>
          ))}
          {overflowCount > 0 && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-surface-elevated text-text-muted">
              +{overflowCount} more
            </span>
          )}
        </div>

        {/* Availability summary */}
        <p className="text-xs text-text-secondary mb-2">
          {availabilitySummary} · {mentor.sessionFormats?.join(', ') || 'Flexible'}
        </p>

        {/* Badges row */}
        <div className="flex flex-wrap gap-1.5">
          {mentor.hasActiveOpportunities && <SharingOpportunitiesBadge />}
          {isUnavailable && <OnBreakBadge returnDate={mentor.returnDate} />}
        </div>
      </div>
    );
  }
);

MentorCard.displayName = 'MentorCard';
