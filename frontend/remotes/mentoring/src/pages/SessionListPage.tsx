/**
 * SessionListPage — Active/completed/pending sessions with tab navigation.
 * Includes confirmation dialog for cancel session (destructive action).
 *
 * Requirements: 6.8, 9.1, 25.5, 25.8, 25.9, 25.10
 */
import { useState } from 'react';
import { Skeleton, ConfirmDialog, EmptyState, ErrorMessage } from '@guided-mentor/design-system';
import { useSessions, useCancelSession } from '../api/mentoring-api';
import type { SessionStatus, SessionSummary } from '../types';

type SessionTab = 'active' | 'completed' | 'pending';

const TAB_STATUS_MAP: Record<SessionTab, SessionStatus | undefined> = {
  active: 'active',
  completed: 'completed',
  pending: 'pending_acceptance',
};

const STATUS_LABELS: Record<SessionStatus, string> = {
  pending_acceptance: 'Pending',
  pending_plan: 'Plan Generating',
  active: 'Active',
  mentee_completed: 'Awaiting Mentor',
  completed: 'Completed',
  unresolved: 'Unresolved',
};

const STATUS_COLORS: Record<SessionStatus, string> = {
  pending_acceptance: 'bg-warning/10 text-warning border-warning/30',
  pending_plan: 'bg-accent/10 text-accent border-accent/30',
  active: 'bg-success/10 text-success border-success/30',
  mentee_completed: 'bg-primary/10 text-primary border-primary/30',
  completed: 'bg-secondary/10 text-secondary border-secondary/30',
  unresolved: 'bg-error/10 text-error border-error/30',
};

function SessionCard({ session, onCancel }: { session: SessionSummary; onCancel: (session: SessionSummary) => void }) {
  const createdDate = new Date(session.createdAt).toLocaleDateString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });

  return (
    <article
      className="glass-card p-4 rounded-lg transition-all duration-base hover:border-primary/20"
      aria-label={`Session with ${session.mentorName || session.menteeName}: ${session.sessionTitle || 'Untitled'}`}
    >
      <div className="flex items-start justify-between mb-3">
        <div className="min-w-0 flex-1">
          <h3 className="text-text-primary font-semibold text-sm truncate">
            {session.sessionTitle || 'Session Plan Pending'}
          </h3>
          <p className="text-text-muted text-xs mt-0.5">
            with {session.mentorName || session.menteeName}
          </p>
        </div>
        <span
          className={[
            'inline-flex items-center text-xs px-2 py-0.5 rounded-full border font-medium shrink-0 ml-2',
            STATUS_COLORS[session.status],
          ].join(' ')}
        >
          {STATUS_LABELS[session.status]}
        </span>
      </div>

      {/* Progress bar for active sessions */}
      {session.checklistProgress !== undefined && session.status === 'active' && (
        <div className="mb-3">
          <div className="flex items-center justify-between mb-1">
            <span className="text-xs text-text-muted">Progress</span>
            <span className="text-xs text-text-secondary">{session.checklistProgress}%</span>
          </div>
          <div className="h-1.5 bg-surface rounded-full overflow-hidden">
            <div
              className="h-full bg-primary rounded-full transition-all duration-base"
              style={{ width: `${session.checklistProgress}%` }}
              role="progressbar"
              aria-valuenow={session.checklistProgress}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-label={`Session progress: ${session.checklistProgress}%`}
            />
          </div>
        </div>
      )}

      <div className="flex items-center justify-between text-xs text-text-muted">
        <time dateTime={session.createdAt}>{createdDate}</time>
        <div className="flex items-center gap-3">
          {(session.status === 'active' || session.status === 'pending_acceptance') && (
            <button
              onClick={() => onCancel(session)}
              className="text-error hover:underline focus-visible:ring-2 focus-visible:ring-error outline-none rounded"
            >
              Cancel
            </button>
          )}
          {session.status === 'active' && (
            <a
              href={`/sessions/${session.sessionId}/plan`}
              className="text-primary hover:underline focus-visible:ring-2 focus-visible:ring-primary outline-none rounded"
            >
              View Plan →
            </a>
          )}
        </div>
      </div>
    </article>
  );
}

export function SessionListPage() {
  const [activeTab, setActiveTab] = useState<SessionTab>('active');
  const [cancelTarget, setCancelTarget] = useState<SessionSummary | null>(null);
  const statusFilter = TAB_STATUS_MAP[activeTab];
  const { data: sessions, isLoading, isError, refetch } = useSessions(statusFilter);
  const cancelSession = useCancelSession();

  const tabs: { key: SessionTab; label: string }[] = [
    { key: 'active', label: 'Active' },
    { key: 'pending', label: 'Pending' },
    { key: 'completed', label: 'Completed' },
  ];

  return (
    <div className="max-w-4xl mx-auto p-6" data-testid="mentoring-session-list-page">
      <h1 className="text-2xl font-bold text-text-primary mb-6">Sessions</h1>

      {/* Tab navigation */}
      <div className="flex gap-1 border-b border-[rgba(255,255,255,0.08)] mb-6" role="tablist">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            role="tab"
            aria-selected={activeTab === tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={[
              'px-4 py-2 text-sm font-medium transition-colors duration-base border-b-2 -mb-px outline-none',
              'focus-visible:ring-2 focus-visible:ring-primary rounded-t-sm',
              activeTab === tab.key
                ? 'border-b-primary text-primary'
                : 'border-b-transparent text-text-secondary hover:text-text-primary',
            ].join(' ')}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div role="tabpanel" aria-label={`${activeTab} sessions`}>
        {/* Loading */}
        {isLoading && (
          <div className="space-y-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} height="6rem" className="rounded-lg" />
            ))}
          </div>
        )}

        {/* Error */}
        {isError && (
          <ErrorMessage
            message="We couldn't load your sessions. Please try again."
            onRetry={() => refetch()}
          />
        )}

        {/* Empty state */}
        {!isLoading && !isError && sessions?.length === 0 && (
          <EmptyState
            icon={<span>{activeTab === 'active' ? '📋' : activeTab === 'pending' ? '⏳' : '✅'}</span>}
            title={`No ${activeTab} sessions`}
            message={
              activeTab === 'active'
                ? 'Browse mentors to start a new mentorship session.'
                : activeTab === 'pending'
                ? 'No pending requests at the moment.'
                : 'Completed sessions will appear here after you finish a mentorship.'
            }
            actionLabel={activeTab === 'active' ? 'Browse Mentors' : undefined}
            actionHref={activeTab === 'active' ? '/browse' : undefined}
          />
        )}

        {/* Session list */}
        {!isLoading && !isError && sessions && sessions.length > 0 && (
          <div className="space-y-3">
            {sessions.map((session) => (
              <SessionCard key={session.sessionId} session={session} onCancel={setCancelTarget} />
            ))}
          </div>
        )}
      </div>

      {/* Cancel Session Confirmation Dialog (Req 25.8) */}
      <ConfirmDialog
        open={cancelTarget !== null}
        onClose={() => setCancelTarget(null)}
        onConfirm={() => {
          if (cancelTarget) {
            cancelSession.mutate(cancelTarget.sessionId);
            setCancelTarget(null);
          }
        }}
        title="Cancel Session"
        description={
          cancelTarget
            ? `Are you sure you want to cancel your session "${cancelTarget.sessionTitle || 'Untitled'}" with ${cancelTarget.mentorName || cancelTarget.menteeName}? This action cannot be undone and both parties will be notified.`
            : ''
        }
        confirmLabel="Cancel Session"
        cancelLabel="Keep Session"
        variant="danger"
        loading={cancelSession.isPending}
      />
    </div>
  );
}

export default SessionListPage;
