/**
 * SessionPlanPage — displays a session plan with timed agenda,
 * prework/followup checklists, and progress bar.
 *
 * Streams the plan in real-time during generation using Vercel AI SDK useObject().
 * Shows loading skeleton for pending-plan state.
 *
 * Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7
 */
import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { experimental_useObject as useObject } from '@ai-sdk/react';
import { z } from 'zod';
import { useSessionPlan, useUpdateChecklist } from '../api/content-api';
import { AgendaTimeline } from '../components/AgendaTimeline';
import { Checklist } from '../components/Checklist';
import { ProgressBar } from '../components/ProgressBar';
import { SessionPlanSkeleton } from '../components/SessionPlanSkeleton';
import type { ChecklistUpdate, SessionPlan } from '../types';

/** Zod schema for streaming structured session plan via useObject */
const sessionPlanSchema = z.object({
  sessionTitle: z.string(),
  agenda: z.array(
    z.object({
      title: z.string(),
      durationMinutes: z.number(),
      description: z.string(),
    })
  ),
  preworkTasks: z.array(z.string()),
  followUpTasks: z.array(z.string()),
});

export function SessionPlanPage() {
  const { sessionId } = useParams<{ sessionId: string }>();

  // Fetch the persisted session plan from the API
  const {
    data: sessionData,
    isLoading,
    error: fetchError,
  } = useSessionPlan(sessionId ?? '');

  const updateChecklist = useUpdateChecklist(sessionId ?? '');

  // Handler for checklist toggle — passed to Checklist component
  const handleChecklistToggle = async (update: ChecklistUpdate): Promise<void> => {
    await updateChecklist.mutateAsync(update);
  };

  // Loading state — initial fetch
  if (isLoading) {
    return <SessionPlanSkeleton />;
  }

  // Fetch error
  if (fetchError) {
    return (
      <div
        className="flex items-center justify-center min-h-[400px]"
        data-testid="content-session-plan-error"
        role="alert"
      >
        <div className="glass-card p-8 text-center max-w-md">
          <p className="text-[var(--color-error)] text-lg font-medium mb-2">
            Failed to load session plan
          </p>
          <p className="text-sm text-[var(--color-text-secondary)]">
            {fetchError.message}
          </p>
        </div>
      </div>
    );
  }

  // Pending plan state — show streaming view
  if (sessionData?.status === 'pending_plan') {
    return <StreamingPlanView sessionId={sessionId ?? ''} />;
  }

  // No plan available yet (edge case)
  if (!sessionData?.plan) {
    return (
      <div
        className="flex items-center justify-center min-h-[400px]"
        data-testid="content-session-plan-empty"
      >
        <div className="glass-card p-8 text-center max-w-md">
          <p className="text-[var(--color-text-secondary)]">
            No session plan available yet.
          </p>
        </div>
      </div>
    );
  }

  // Active plan view — full session plan with interactive checklists
  const { plan, checklistState } = sessionData;
  const totalChecked =
    checklistState.prework.filter(Boolean).length +
    checklistState.followup.filter(Boolean).length;
  const totalItems = checklistState.prework.length + checklistState.followup.length;

  return (
    <div
      className="flex flex-col gap-8 max-w-4xl mx-auto py-8 px-6"
      data-testid="content-session-plan-page"
    >
      {/* Session Title */}
      <header>
        <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">
          {plan.sessionTitle}
        </h1>
      </header>

      {/* Progress Bar */}
      <ProgressBar
        checkedCount={totalChecked}
        totalCount={totalItems}
        className="glass-card p-4"
      />

      {/* Timed Agenda */}
      <AgendaTimeline items={plan.agenda} />

      {/* Pre-work Checklist */}
      <Checklist
        title="Pre-work"
        items={plan.preworkTasks}
        checkedState={checklistState.prework}
        type="prework"
        onToggle={handleChecklistToggle}
        className="glass-card p-4"
      />

      {/* Follow-up Checklist */}
      <Checklist
        title="Follow-up"
        items={plan.followUpTasks}
        checkedState={checklistState.followup}
        type="followup"
        onToggle={handleChecklistToggle}
        className="glass-card p-4"
      />
    </div>
  );
}

/**
 * StreamingPlanView — renders while the plan is being generated.
 * Uses useObject() to stream partial JSON from the backend SSE endpoint.
 * Triggers stream on mount via submit().
 */
function StreamingPlanView({ sessionId }: { sessionId: string }) {
  const { object: streamingPlan, submit, isLoading: isStreaming, error } = useObject({
    api: `/v1/sessions/${sessionId}/plan/stream`,
    schema: sessionPlanSchema,
  });

  // Trigger streaming on mount
  useEffect(() => {
    submit(undefined);
  }, [submit]);

  // Show skeleton if nothing has streamed yet
  if (!streamingPlan && isStreaming) {
    return (
      <div data-testid="content-session-plan-pending">
        <div className="max-w-4xl mx-auto py-8 px-6 mb-6">
          <p
            className="text-[var(--color-text-secondary)] text-sm animate-pulse"
            aria-live="polite"
          >
            Your session plan is being generated…
          </p>
        </div>
        <SessionPlanSkeleton />
      </div>
    );
  }

  // If nothing is streaming and no data yet, show skeleton
  if (!streamingPlan && !isStreaming) {
    return (
      <div data-testid="content-session-plan-pending">
        <div className="max-w-4xl mx-auto py-8 px-6 mb-6">
          <p
            className="text-[var(--color-text-secondary)] text-sm"
            aria-live="polite"
          >
            Your session plan is being generated…
          </p>
        </div>
        <SessionPlanSkeleton />
      </div>
    );
  }

  const plan = streamingPlan as Partial<SessionPlan> | undefined;

  return (
    <div
      className="flex flex-col gap-8 max-w-4xl mx-auto py-8 px-6"
      data-testid="content-session-plan-streaming"
    >
      {/* Streaming indicator */}
      {isStreaming && (
        <div className="flex items-center gap-2" aria-live="polite">
          <div className="h-2 w-2 rounded-full bg-[var(--color-primary)] animate-pulse" />
          <span className="text-sm text-[var(--color-text-secondary)]">
            Generating your session plan…
          </span>
        </div>
      )}

      {error && (
        <div role="alert" className="text-sm text-[var(--color-error)]">
          Stream error: {error.message}
        </div>
      )}

      {/* Session Title (streamed) */}
      {plan?.sessionTitle && (
        <header>
          <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">
            {plan.sessionTitle}
          </h1>
        </header>
      )}

      {/* Agenda items (streamed incrementally) */}
      {plan?.agenda && plan.agenda.length > 0 && (
        <AgendaTimeline items={plan.agenda} />
      )}

      {/* Pre-work (streamed — read-only during generation) */}
      {plan?.preworkTasks && plan.preworkTasks.length > 0 && (
        <section aria-label="Pre-work" className="glass-card p-4">
          <h3 className="text-lg font-semibold text-[var(--color-text-primary)] mb-3">
            Pre-work
          </h3>
          <ul className="flex flex-col gap-2" role="list">
            {plan.preworkTasks.map((task, i) => (
              <li
                key={i}
                className="flex items-center gap-3 p-3 text-sm text-[var(--color-text-primary)]"
              >
                <span
                  className="h-5 w-5 rounded border-2 border-[rgba(255,255,255,0.2)] flex-shrink-0"
                  aria-hidden="true"
                />
                {task}
              </li>
            ))}
          </ul>
        </section>
      )}

      {/* Follow-up (streamed — read-only during generation) */}
      {plan?.followUpTasks && plan.followUpTasks.length > 0 && (
        <section aria-label="Follow-up" className="glass-card p-4">
          <h3 className="text-lg font-semibold text-[var(--color-text-primary)] mb-3">
            Follow-up
          </h3>
          <ul className="flex flex-col gap-2" role="list">
            {plan.followUpTasks.map((task, i) => (
              <li
                key={i}
                className="flex items-center gap-3 p-3 text-sm text-[var(--color-text-primary)]"
              >
                <span
                  className="h-5 w-5 rounded border-2 border-[rgba(255,255,255,0.2)] flex-shrink-0"
                  aria-hidden="true"
                />
                {task}
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}

export default SessionPlanPage;
