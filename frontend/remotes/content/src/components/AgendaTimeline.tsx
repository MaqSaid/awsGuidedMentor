/**
 * AgendaTimeline — displays a timed agenda with cards for each item.
 * Each card shows title, duration label, and description.
 *
 * Requirements: 8.1, 8.2
 */
import type { AgendaItem } from '../types';

export interface AgendaTimelineProps {
  items: AgendaItem[];
  className?: string;
}

export function AgendaTimeline({ items, className = '' }: AgendaTimelineProps) {
  return (
    <section aria-label="Session Agenda" className={className}>
      <h3 className="text-lg font-semibold text-[var(--color-text-primary)] mb-4">
        Timed Agenda
      </h3>
      <ol className="flex flex-col gap-3" role="list">
        {items.map((item, index) => (
          <li key={index}>
            <article
              className="glass-card p-4 flex flex-col gap-2"
              aria-label={`Agenda item ${index + 1}: ${item.title}`}
            >
              <div className="flex items-center justify-between">
                <h4 className="text-base font-medium text-[var(--color-text-primary)]">
                  {item.title}
                </h4>
                <span
                  className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-[var(--color-primary)]/10 text-[var(--color-primary)]"
                  aria-label={`${item.durationMinutes} minutes`}
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="12"
                    height="12"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    aria-hidden="true"
                  >
                    <circle cx="12" cy="12" r="10" />
                    <polyline points="12 6 12 12 16 14" />
                  </svg>
                  {item.durationMinutes} min
                </span>
              </div>
              <p className="text-sm text-[var(--color-text-secondary)]">
                {item.description}
              </p>
            </article>
          </li>
        ))}
      </ol>
    </section>
  );
}
