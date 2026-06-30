/**
 * OpportunityNotificationPreferences — Mentee settings section
 * with type filter checkboxes + skill-match toggle.
 *
 * Requirements: 28.13
 */
import { useCallback } from 'react';
import { Skeleton } from '@guided-mentor/design-system';
import { useOpportunityPreferences, useUpdateOpportunityPreferences } from '../api/mentoring-api';
import type { OpportunityType } from '../types';

interface OpportunityNotificationPreferencesProps {
  className?: string;
}

const OPPORTUNITY_TYPES: { value: OpportunityType; label: string; description: string }[] = [
  { value: 'job', label: 'Jobs', description: 'Job openings and career opportunities' },
  { value: 'workshop', label: 'Workshops', description: 'Hands-on skill-building workshops' },
  { value: 'event', label: 'Events', description: 'Community meetups and conferences' },
  { value: 'training', label: 'Training', description: 'Training programs and courses' },
];

export function OpportunityNotificationPreferences({ className = '' }: OpportunityNotificationPreferencesProps) {
  const { data: prefs, isLoading } = useOpportunityPreferences();
  const updatePrefs = useUpdateOpportunityPreferences();

  const handleTypeToggle = useCallback(
    (type: OpportunityType) => {
      if (!prefs) return;
      const current = prefs.enabledTypes;
      const updated = current.includes(type)
        ? current.filter((t) => t !== type)
        : [...current, type];
      updatePrefs.mutate({ ...prefs, enabledTypes: updated });
    },
    [prefs, updatePrefs]
  );

  const handleSkillMatchToggle = useCallback(() => {
    if (!prefs) return;
    updatePrefs.mutate({ ...prefs, skillMatchEnabled: !prefs.skillMatchEnabled });
  }, [prefs, updatePrefs]);

  if (isLoading) {
    return (
      <div className={['glass-card p-4 rounded-lg space-y-3', className].join(' ')}>
        <Skeleton height="1.25rem" width="60%" />
        <Skeleton height="2rem" />
        <Skeleton height="2rem" />
        <Skeleton height="2rem" />
      </div>
    );
  }

  if (!prefs) return null;

  return (
    <div className={['glass-card p-4 rounded-lg space-y-4', className].join(' ')}>
      <div>
        <h3 className="text-sm font-semibold text-text-primary mb-1">
          Opportunity Notifications
        </h3>
        <p className="text-xs text-text-muted">
          Choose which opportunity types you'd like to be notified about.
        </p>
      </div>

      {/* Type filter checkboxes */}
      <fieldset>
        <legend className="sr-only">Opportunity types to receive notifications for</legend>
        <div className="space-y-2">
          {OPPORTUNITY_TYPES.map(({ value, label, description }) => (
            <label
              key={value}
              className="flex items-start gap-3 py-2 px-3 rounded-md hover:bg-white/5 cursor-pointer"
            >
              <input
                type="checkbox"
                checked={prefs.enabledTypes.includes(value)}
                onChange={() => handleTypeToggle(value)}
                className="accent-primary mt-0.5"
                aria-label={`Receive notifications for ${label}`}
              />
              <div>
                <span className="text-sm text-text-primary">{label}</span>
                <p className="text-xs text-text-muted">{description}</p>
              </div>
            </label>
          ))}
        </div>
      </fieldset>

      {/* Skill match toggle */}
      <div className="flex items-center justify-between pt-3 border-t border-[rgba(255,255,255,0.08)]">
        <div>
          <p className="text-sm font-medium text-text-primary">Skill-match only</p>
          <p className="text-xs text-text-muted">
            Only notify me about opportunities matching my skills
          </p>
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={prefs.skillMatchEnabled}
          aria-label="Enable skill-match filtering for opportunity notifications"
          onClick={handleSkillMatchToggle}
          disabled={updatePrefs.isPending}
          className={[
            'relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-base',
            'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none',
            prefs.skillMatchEnabled ? 'bg-primary' : 'bg-[rgba(255,255,255,0.1)]',
            updatePrefs.isPending ? 'opacity-50 cursor-not-allowed' : '',
          ].join(' ')}
        >
          <span
            className={[
              'inline-block h-4 w-4 rounded-full bg-white transition-transform duration-base',
              prefs.skillMatchEnabled ? 'translate-x-6' : 'translate-x-1',
            ].join(' ')}
          />
        </button>
      </div>

      {updatePrefs.isPending && (
        <p className="text-xs text-text-muted" aria-live="polite">Saving preferences...</p>
      )}
    </div>
  );
}
