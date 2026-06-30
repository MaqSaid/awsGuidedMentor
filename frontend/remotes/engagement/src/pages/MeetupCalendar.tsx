/**
 * MeetupCalendar — Upcoming meetups display, MeetupAlignModal for session
 * scheduling, and MeetupBadge ("Attending [meetup]").
 *
 * Requirements: 29.2, 29.3, 29.6, 29.9
 */
import { useState } from 'react';
import { useUpcomingMeetups } from '../hooks/useApi';
import type { MeetupEvent } from '../types';

interface MeetupCalendarProps {
  chapter?: string;
}

export function MeetupCalendar({ chapter }: MeetupCalendarProps) {
  const { data: meetups, isLoading, isError, refetch } = useUpcomingMeetups(chapter);

  if (isLoading) {
    return (
      <div data-testid="engagement-meetup-calendar" className="p-6 space-y-4">
        <h2 className="text-xl font-bold text-text-primary">Upcoming Meetups</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="glass-card p-4 rounded-lg animate-pulse h-40 bg-[rgba(255,255,255,0.06)]" role="status" aria-label="Loading" />
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div data-testid="engagement-meetup-calendar" className="p-6 space-y-4">
        <h2 className="text-xl font-bold text-text-primary">Upcoming Meetups</h2>
        <div className="glass-card p-6 text-center" role="alert">
          <p className="text-text-secondary mb-4">Failed to load meetup events.</p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-primary text-[#0a1628] font-semibold rounded-md hover:opacity-90 transition-opacity"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  const events = meetups ?? [];

  return (
    <div data-testid="engagement-meetup-calendar" className="p-6 space-y-4">
      <h2 className="text-xl font-bold text-text-primary">Upcoming Meetups</h2>

      {events.length === 0 ? (
        <div className="glass-card p-8 rounded-lg text-center">
          <p className="text-text-muted">No upcoming meetups for your chapter. Check back soon!</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {events.map((meetup) => (
            <MeetupEventCard key={meetup.meetupId} meetup={meetup} />
          ))}
        </div>
      )}
    </div>
  );
}

function MeetupEventCard({ meetup }: { meetup: MeetupEvent }) {
  const [showAlignModal, setShowAlignModal] = useState(false);

  return (
    <>
      <div className="glass-card p-5 rounded-lg">
        <div className="flex items-start justify-between">
          <h3 className="text-sm font-semibold text-text-primary">{meetup.title}</h3>
          <span className="text-xs px-2 py-0.5 rounded-full bg-primary/10 text-primary font-medium flex-shrink-0 ml-2">
            {meetup.chapter}
          </span>
        </div>

        <div className="mt-3 space-y-1.5">
          <p className="text-xs text-text-secondary flex items-center gap-1.5">
            <span aria-hidden="true">📅</span>
            {new Date(meetup.date).toLocaleDateString('en-AU', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
          <p className="text-xs text-text-secondary flex items-center gap-1.5">
            <span aria-hidden="true">🕐</span>
            {meetup.startTime} – {meetup.endTime}
          </p>
          <p className="text-xs text-text-secondary flex items-center gap-1.5">
            <span aria-hidden="true">📍</span>
            {meetup.venueName}
          </p>
          <p className="text-xs text-text-muted">{meetup.venueAddress}</p>
        </div>

        <div className="mt-3 flex items-center justify-between">
          <span className="text-xs text-primary">{meetup.confirmedAttendees} mentors confirmed</span>
          <button
            onClick={() => setShowAlignModal(true)}
            className="text-xs font-medium text-primary hover:text-primary/80 underline transition-colors focus-visible:ring-2 focus-visible:ring-primary outline-none rounded-sm px-1"
            aria-label={`Schedule session at ${meetup.title}`}
          >
            Align session
          </button>
        </div>

        {meetup.eventUrl && (
          <a
            href={meetup.eventUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="mt-2 block text-xs text-text-muted hover:text-text-secondary underline transition-colors"
            aria-label={`View ${meetup.title} event details (opens in new tab)`}
          >
            View event details ↗
          </a>
        )}
      </div>

      {showAlignModal && (
        <MeetupAlignModal meetup={meetup} onClose={() => setShowAlignModal(false)} />
      )}
    </>
  );
}

/**
 * MeetupAlignModal — Modal for scheduling a session aligned with a meetup event.
 */
function MeetupAlignModal({ meetup, onClose }: { meetup: MeetupEvent; onClose: () => void }) {
  const [scheduling, setScheduling] = useState(false);
  const [scheduled, setScheduled] = useState(false);

  const handleSchedule = async () => {
    setScheduling(true);
    try {
      await fetch('/v1/sessions/align-meetup', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ meetupId: meetup.meetupId }),
      });
      setScheduled(true);
    } catch {
      // Error handled silently — user can retry
    } finally {
      setScheduling(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[9998] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-[rgba(0,0,0,0.6)] backdrop-blur-sm" onClick={onClose} aria-hidden="true" />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="meetup-align-title"
        className="relative z-[9999] glass-card p-6 rounded-lg w-full max-w-md shadow-2xl"
      >
        <h3 id="meetup-align-title" className="text-lg font-semibold text-text-primary mb-2">
          Schedule at Meetup
        </h3>
        <p className="text-sm text-text-secondary mb-4">
          Align your mentoring session with <strong className="text-text-primary">{meetup.title}</strong>?
        </p>

        <div className="glass-card p-3 rounded-md mb-4 bg-[rgba(255,255,255,0.03)]">
          <p className="text-xs text-text-secondary">
            📅 {new Date(meetup.date).toLocaleDateString('en-AU', { weekday: 'long', month: 'long', day: 'numeric' })}
          </p>
          <p className="text-xs text-text-secondary mt-1">
            🕐 {meetup.startTime} – {meetup.endTime}
          </p>
          <p className="text-xs text-text-secondary mt-1">
            📍 {meetup.venueName}, {meetup.venueAddress}
          </p>
        </div>

        {scheduled ? (
          <div className="text-center py-2">
            <p className="text-sm text-success font-medium">✓ Session aligned with meetup!</p>
            <button
              onClick={onClose}
              className="mt-3 px-4 py-2 text-sm bg-primary text-[#0a1628] font-medium rounded-md hover:opacity-90 transition-opacity"
            >
              Done
            </button>
          </div>
        ) : (
          <div className="flex gap-3 justify-end">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm border border-[rgba(255,255,255,0.2)] text-text-secondary rounded-md hover:text-text-primary transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleSchedule}
              disabled={scheduling}
              className="px-4 py-2 text-sm bg-primary text-[#0a1628] font-medium rounded-md hover:opacity-90 disabled:opacity-50 transition-opacity"
            >
              {scheduling ? 'Scheduling...' : 'Schedule at this meetup'}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

/**
 * MeetupBadge — Shows "Attending [meetup]" on mentor cards.
 * Requirements: 29.6
 */
export function MeetupBadge({ meetupTitle }: { meetupTitle: string }) {
  return (
    <span
      className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium"
      aria-label={`Attending ${meetupTitle}`}
    >
      <span aria-hidden="true">📍</span>
      Attending {meetupTitle}
    </span>
  );
}

export default MeetupCalendar;
