/**
 * SessionPlanSkeleton — loading skeleton displayed while the session plan is
 * loading or being generated (pending-plan state).
 *
 * Requirements: 8.6
 */

export function SessionPlanSkeleton() {
  return (
    <div
      className="flex flex-col gap-8 max-w-4xl mx-auto py-8 px-6"
      data-testid="content-session-plan-skeleton"
      role="status"
      aria-label="Loading session plan"
      aria-busy="true"
    >
      {/* Title skeleton */}
      <div className="h-8 w-2/3 rounded-md bg-[rgba(255,255,255,0.06)] animate-pulse" />

      {/* Progress bar skeleton */}
      <div className="glass-card p-4 flex flex-col gap-2">
        <div className="flex justify-between">
          <div className="h-4 w-16 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
          <div className="h-4 w-8 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
        </div>
        <div className="h-2.5 w-full rounded-full bg-[rgba(255,255,255,0.06)] animate-pulse" />
      </div>

      {/* Agenda skeleton — 4 cards */}
      <section aria-label="Loading agenda">
        <div className="h-6 w-32 rounded bg-[rgba(255,255,255,0.06)] animate-pulse mb-4" />
        <div className="flex flex-col gap-3">
          {Array.from({ length: 4 }, (_, i) => (
            <div key={i} className="glass-card p-4 flex flex-col gap-2">
              <div className="flex justify-between">
                <div className="h-5 w-48 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
                <div className="h-5 w-16 rounded-full bg-[rgba(255,255,255,0.06)] animate-pulse" />
              </div>
              <div className="h-4 w-full rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
              <div className="h-4 w-3/4 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
            </div>
          ))}
        </div>
      </section>

      {/* Pre-work checklist skeleton */}
      <div className="glass-card p-4 flex flex-col gap-3">
        <div className="h-6 w-24 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
        {Array.from({ length: 3 }, (_, i) => (
          <div key={i} className="flex items-center gap-3 p-3">
            <div className="h-5 w-5 rounded bg-[rgba(255,255,255,0.06)] animate-pulse flex-shrink-0" />
            <div className="h-4 w-64 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
          </div>
        ))}
      </div>

      {/* Follow-up checklist skeleton */}
      <div className="glass-card p-4 flex flex-col gap-3">
        <div className="h-6 w-24 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
        {Array.from({ length: 3 }, (_, i) => (
          <div key={i} className="flex items-center gap-3 p-3">
            <div className="h-5 w-5 rounded bg-[rgba(255,255,255,0.06)] animate-pulse flex-shrink-0" />
            <div className="h-4 w-64 rounded bg-[rgba(255,255,255,0.06)] animate-pulse" />
          </div>
        ))}
      </div>
    </div>
  );
}
