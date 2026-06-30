/**
 * ProgressBar — displays checklist completion as a percentage.
 * Calculated as round((checked / total) × 100)%.
 *
 * Requirements: 8.5
 */

export interface ProgressBarProps {
  checkedCount: number;
  totalCount: number;
  className?: string;
}

export function calculateProgress(checkedCount: number, totalCount: number): number {
  if (totalCount === 0) return 0;
  return Math.round((checkedCount / totalCount) * 100);
}

export function ProgressBar({ checkedCount, totalCount, className = '' }: ProgressBarProps) {
  const percent = calculateProgress(checkedCount, totalCount);

  return (
    <div className={`flex flex-col gap-2 ${className}`}>
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-[var(--color-text-secondary)]">
          Progress
        </span>
        <span className="text-sm font-semibold text-[var(--color-text-primary)]">
          {percent}%
        </span>
      </div>
      <div
        role="progressbar"
        aria-valuenow={percent}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`Checklist progress: ${percent}% complete`}
        className="h-2.5 w-full rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden"
      >
        <div
          className="h-full rounded-full bg-[var(--color-primary)] transition-all"
          style={{
            width: `${percent}%`,
            transitionDuration: 'var(--transition-base)',
          }}
        />
      </div>
      <span className="text-xs text-[var(--color-text-muted)]">
        {checkedCount} of {totalCount} items completed
      </span>
    </div>
  );
}
