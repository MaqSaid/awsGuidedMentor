/**
 * MentorDashboard — Displays pending requests (accept/decline), active mentees,
 * capacity indicator, availability toggle, and upcoming meetups (3).
 *
 * Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 25.5, 25.8, 25.9, 25.10, 29.9, 32.8
 */
import { useState } from 'react';
import { useMentorDashboard, useAcceptRequest, useDeclineRequest, useToggleAvailability } from '../hooks/useApi';
import { EmptyState, ErrorMessage, ConfirmDialog } from '@guided-mentor/design-system/components';
import type { PendingRequest, ActiveMentee, MeetupEvent } from '../types';

export function MentorDashboard() {
  const { data, isLoading, isError, refetch } = useMentorDashboard();
  const acceptMutation = useAcceptRequest();
  const declineMutation = useDeclineRequest();
  const toggleAvailability = useToggleAvailability();

  if (isLoading) {
    return (
      <div data-testid="engagement-mentor-dashboard" className="p-6 space-y-6">
        <h1 className="text-2xl font-bold text-text-primary">Mentor Dashboard</h1>
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="glass-card p-4 animate-pulse h-24 rounded-lg bg-[rgba(255,255,255,0.06)]" role="status" aria-label="Loading" />
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div data-testid="engagement-mentor-dashboard" className="p-6 space-y-6">
        <h1 className="text-2xl font-bold text-text-primary">Mentor Dashboard</h1>
        <ErrorMessage
          message="We couldn't load your dashboard data. Please try again."
          onRetry={() => refetch()}
        />
      </div>
    );
  }

  const { pendingRequests, activeMentees, activeMenteeCount, maxMentees, isAvailable, upcomingMeetups } = data!;
  const capacityPercent = Math.round((activeMenteeCount / maxMentees) * 100);
  const atCapacity = activeMenteeCount >= maxMentees;

  // Confirmation dialog state for decline (destructive action - Req 25.8)
  const [declineTarget, setDeclineTarget] = useState<PendingRequest | null>(null);

  const handleDeclineConfirm = () => {
    if (declineTarget) {
      declineMutation.mutate(declineTarget.sessionId);
      setDeclineTarget(null);
    }
  };

  return (
    <div data-testid="engagement-mentor-dashboard" className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-text-primary">Mentor Dashboard</h1>
        <AvailabilityToggle
          isAvailable={isAvailable}
          onToggle={(val) => toggleAvailability.mutate(val)}
          loading={toggleAvailability.isPending}
        />
      </div>

      {/* Capacity Indicator */}
      <section aria-label="Capacity indicator" className="glass-card p-4 rounded-lg">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm text-text-secondary">Active Mentees</span>
          <span className="text-sm font-semibold text-text-primary">
            {activeMenteeCount} / {maxMentees}
          </span>
        </div>
        <div className="h-2 w-full rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden">
          <div
            className={`h-full rounded-full transition-all ${atCapacity ? 'bg-error' : 'bg-primary'}`}
            style={{ width: `${capacityPercent}%` }}
          />
        </div>
        {atCapacity && (
          <p className="text-xs text-error mt-1">You've reached maximum capacity.</p>
        )}
      </section>

      {/* Pending Requests */}
      <section aria-label="Pending mentorship requests">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Pending Requests</h2>
        {pendingRequests.length === 0 ? (
          <EmptyState
            icon={<span>📬</span>}
            message="No pending requests at this time. New requests from mentees will appear here."
          />
        ) : (
          <div className="space-y-3">
            {pendingRequests.map((request) => (
              <PendingRequestCard
                key={request.sessionId}
                request={request}
                onAccept={() => acceptMutation.mutate(request.sessionId)}
                onDecline={() => setDeclineTarget(request)}
                atCapacity={atCapacity}
                acceptLoading={acceptMutation.isPending}
                declineLoading={declineMutation.isPending}
              />
            ))}
          </div>
        )}
      </section>

      {/* Active Mentees */}
      <section aria-label="Active mentees">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Active Mentees</h2>
        {activeMentees.length === 0 ? (
          <EmptyState
            icon={<span>👥</span>}
            message="No active mentees yet. Accept incoming requests above to start mentoring."
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {activeMentees.map((mentee) => (
              <ActiveMenteeCard key={mentee.sessionId} mentee={mentee} />
            ))}
          </div>
        )}
      </section>

      {/* Upcoming Meetups */}
      <section aria-label="Upcoming meetups">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Upcoming Meetups</h2>
        {upcomingMeetups.length === 0 ? (
          <p className="text-text-muted text-sm">No upcoming meetups for your chapter.</p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {upcomingMeetups.slice(0, 3).map((meetup) => (
              <MeetupCardSmall key={meetup.meetupId} meetup={meetup} />
            ))}
          </div>
        )}
      </section>

      {/* Confirmation Dialog for Decline (destructive action - Req 25.8) */}
      <ConfirmDialog
        open={declineTarget !== null}
        onClose={() => setDeclineTarget(null)}
        onConfirm={handleDeclineConfirm}
        title="Decline Mentorship Request"
        description={
          declineTarget
            ? `Are you sure you want to decline the request from ${declineTarget.menteeName}? They will be notified and the request will be removed permanently.`
            : ''
        }
        confirmLabel="Decline Request"
        cancelLabel="Keep Request"
        variant="danger"
        loading={declineMutation.isPending}
      />
    </div>
  );
}

function AvailabilityToggle({
  isAvailable,
  onToggle,
  loading,
}: {
  isAvailable: boolean;
  onToggle: (val: boolean) => void;
  loading: boolean;
}) {
  return (
    <button
      onClick={() => onToggle(!isAvailable)}
      disabled={loading}
      aria-pressed={isAvailable}
      aria-label={`Availability: ${isAvailable ? 'Available' : 'Unavailable'}. Click to toggle.`}
      className={`
        relative inline-flex h-8 w-16 items-center rounded-full transition-colors
        ${isAvailable ? 'bg-success' : 'bg-[rgba(255,255,255,0.1)]'}
        ${loading ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
      `}
    >
      <span
        className={`
          inline-block h-6 w-6 transform rounded-full bg-white transition-transform shadow-sm
          ${isAvailable ? 'translate-x-9' : 'translate-x-1'}
        `}
      />
      <span className="sr-only">{isAvailable ? 'Available' : 'Unavailable'}</span>
    </button>
  );
}

function PendingRequestCard({
  request,
  onAccept,
  onDecline,
  atCapacity,
  acceptLoading,
  declineLoading,
}: {
  request: PendingRequest;
  onAccept: () => void;
  onDecline: () => void;
  atCapacity: boolean;
  acceptLoading: boolean;
  declineLoading: boolean;
}) {
  return (
    <div className="glass-card p-4 rounded-lg flex items-center gap-4">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-medium text-text-primary">{request.menteeName}</h3>
          <span className="text-xs font-bold text-primary">{request.compatibilityScore}% match</span>
        </div>
        <p className="text-xs text-text-muted mt-0.5 truncate">Goal: {request.menteeGoal}</p>
        <p className="text-xs text-text-muted">
          Requested {new Date(request.requestedAt).toLocaleDateString('en-AU')}
        </p>
      </div>
      <div className="flex gap-2 flex-shrink-0">
        <button
          onClick={onAccept}
          disabled={atCapacity || acceptLoading}
          title={atCapacity ? 'Cannot accept — at maximum capacity' : 'Accept request'}
          aria-label={`Accept request from ${request.menteeName}`}
          className="px-3 py-1.5 text-sm bg-success text-[#0a1628] font-medium rounded-md hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed transition-opacity"
        >
          {acceptLoading ? '...' : 'Accept'}
        </button>
        <button
          onClick={onDecline}
          disabled={declineLoading}
          aria-label={`Decline request from ${request.menteeName}`}
          className="px-3 py-1.5 text-sm border border-[rgba(255,255,255,0.2)] text-text-secondary rounded-md hover:border-error hover:text-error disabled:opacity-50 transition-colors"
        >
          {declineLoading ? '...' : 'Decline'}
        </button>
      </div>
    </div>
  );
}

function ActiveMenteeCard({ mentee }: { mentee: ActiveMentee }) {
  return (
    <a
      href={`/sessions/${mentee.sessionId}`}
      className="glass-card p-4 rounded-lg hover:ring-1 hover:ring-primary/30 transition-all block"
    >
      <h3 className="text-sm font-medium text-text-primary">{mentee.menteeName}</h3>
      <p className="text-xs text-text-muted mt-1 truncate">{mentee.sessionTitle}</p>
      <div className="flex items-center justify-between mt-3">
        <span className="text-xs text-text-secondary capitalize">{mentee.status.replace('_', ' ')}</span>
        <span className="text-xs font-semibold text-primary">{mentee.progressPercent}%</span>
      </div>
      <div className="mt-2 h-1.5 w-full rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden">
        <div
          className="h-full rounded-full bg-primary transition-all"
          style={{ width: `${mentee.progressPercent}%` }}
        />
      </div>
    </a>
  );
}

function MeetupCardSmall({ meetup }: { meetup: MeetupEvent }) {
  return (
    <div className="glass-card p-4 rounded-lg">
      <h3 className="text-sm font-medium text-text-primary truncate">{meetup.title}</h3>
      <p className="text-xs text-text-muted mt-1">{meetup.chapter}</p>
      <p className="text-xs text-text-secondary mt-1">
        {new Date(meetup.date).toLocaleDateString('en-AU', { weekday: 'short', month: 'short', day: 'numeric' })}
        {' · '}{meetup.startTime} – {meetup.endTime}
      </p>
      <p className="text-xs text-text-muted mt-1">{meetup.venueName}</p>
      <p className="text-xs text-primary mt-2">{meetup.confirmedAttendees} mentors attending</p>
    </div>
  );
}

export default MentorDashboard;
