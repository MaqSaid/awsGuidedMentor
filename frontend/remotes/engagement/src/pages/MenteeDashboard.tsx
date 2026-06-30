/**
 * MenteeDashboard — Displays active session cards, top 3 mentors,
 * progress stats, summary bar, upcoming meetups (3), and empty states.
 *
 * Requirements: 10.1, 10.3, 10.4, 10.5, 25.5, 25.9, 25.10, 29.9
 */
import { useMenteeDashboard } from '../hooks/useApi';
import { EmptyState, ErrorMessage } from '@guided-mentor/design-system/components';
import type { SessionCard, MentorRecommendation, MeetupEvent } from '../types';

export function MenteeDashboard() {
  const { data, isLoading, isError, refetch } = useMenteeDashboard();

  if (isLoading) {
    return (
      <div data-testid="engagement-mentee-dashboard" className="p-6 space-y-6">
        <h1 className="text-2xl font-bold text-text-primary">Mentee Dashboard</h1>
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
      <div data-testid="engagement-mentee-dashboard" className="p-6 space-y-6">
        <h1 className="text-2xl font-bold text-text-primary">Mentee Dashboard</h1>
        <ErrorMessage
          message="We couldn't load your dashboard data. Please try again."
          onRetry={() => refetch()}
        />
      </div>
    );
  }

  const { activeSessions, topMentors, progressStats } = data!;

  return (
    <div data-testid="engagement-mentee-dashboard" className="p-6 space-y-6">
      <h1 className="text-2xl font-bold text-text-primary">Mentee Dashboard</h1>

      {/* Summary Bar */}
      <section aria-label="Session summary" className="glass-card p-4 rounded-lg">
        <div className="flex items-center gap-6">
          <SummaryItem label="Completed" value={progressStats.completedSessions} />
          <SummaryItem label="In Progress" value={progressStats.inProgressSessions} />
          <SummaryItem label="Pending" value={progressStats.pendingRequests} />
          <SummaryItem label="Checklist Done" value={progressStats.totalChecklistCompleted} />
          <div className="ml-auto flex items-center gap-2">
            <span className="text-sm text-text-muted">Overall</span>
            <span className="text-lg font-bold text-primary">{progressStats.overallCompletionPercent}%</span>
          </div>
        </div>
      </section>

      {/* Active Sessions */}
      <section aria-label="Active sessions">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Active Sessions</h2>
        {activeSessions.length === 0 ? (
          <EmptyState
            icon={<span>📋</span>}
            title="No Active Sessions"
            message="You don't have any active sessions yet. Browse available mentors to get started with your first mentoring session."
            actionLabel="Browse Mentors"
            actionHref="/browse"
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {activeSessions.map((session) => (
              <SessionCardComponent key={session.sessionId} session={session} />
            ))}
          </div>
        )}
      </section>

      {/* Top 3 Recommended Mentors */}
      <section aria-label="Recommended mentors">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Top Recommended Mentors</h2>
        {topMentors.length === 0 ? (
          <EmptyState
            icon={<span>🎓</span>}
            title="No Recommendations Yet"
            message="We're working on finding the best mentors for you. Complete your profile to improve recommendations."
            actionLabel="Browse All Mentors"
            actionHref="/browse"
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {topMentors.slice(0, 3).map((mentor) => (
              <MentorCard key={mentor.mentorId} mentor={mentor} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function SummaryItem({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex flex-col items-center">
      <span className="text-2xl font-bold text-text-primary">{value}</span>
      <span className="text-xs text-text-muted">{label}</span>
    </div>
  );
}

function SessionCardComponent({ session }: { session: SessionCard }) {
  return (
    <a
      href={`/sessions/${session.sessionId}`}
      className="glass-card p-4 rounded-lg hover:ring-1 hover:ring-primary/30 transition-all block"
    >
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-sm font-medium text-text-primary truncate">{session.sessionTitle}</h3>
        <span className="text-xs text-primary font-semibold">{session.progressPercent}%</span>
      </div>
      <p className="text-xs text-text-muted mb-2">with {session.mentorName}</p>
      {session.nextTask && (
        <p className="text-xs text-text-secondary">
          Next: <span className="text-text-primary">{session.nextTask}</span>
        </p>
      )}
      {/* Progress bar */}
      <div className="mt-3 h-1.5 w-full rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden">
        <div
          className="h-full rounded-full bg-primary transition-all"
          style={{ width: `${session.progressPercent}%` }}
        />
      </div>
    </a>
  );
}

function MentorCard({ mentor }: { mentor: MentorRecommendation }) {
  return (
    <a
      href="/browse"
      className="glass-card p-4 rounded-lg hover:ring-1 hover:ring-primary/30 transition-all block"
    >
      <div className="flex items-center gap-3 mb-2">
        <div className="w-10 h-10 rounded-full bg-[rgba(255,255,255,0.1)] flex items-center justify-center text-text-primary font-bold text-sm">
          {mentor.displayName.charAt(0)}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-text-primary truncate">{mentor.displayName}</p>
          <p className="text-xs text-text-muted truncate">{mentor.professionalTitle}</p>
        </div>
        <span className="text-sm font-bold text-primary">{mentor.compatibilityScore}%</span>
      </div>
      <p className="text-xs text-text-muted">{mentor.chapter}</p>
      <div className="mt-2 flex flex-wrap gap-1">
        {mentor.expertiseAreas.slice(0, 3).map((area) => (
          <span key={area} className="text-xs px-2 py-0.5 rounded-full bg-[rgba(255,255,255,0.06)] text-text-secondary">
            {area}
          </span>
        ))}
      </div>
    </a>
  );
}

function MeetupCard({ meetup }: { meetup: MeetupEvent }) {
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

export default MenteeDashboard;
