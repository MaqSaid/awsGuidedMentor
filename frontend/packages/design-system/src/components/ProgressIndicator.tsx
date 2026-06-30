import type { HTMLAttributes } from 'react';

/**
 * ProgressIndicator component showing step progress (Step X of Y) with visual bar.
 * Used for onboarding wizards and multi-step flows.
 *
 * Requirements: 18.3, 25.6
 */

export interface ProgressIndicatorProps extends HTMLAttributes<HTMLDivElement> {
  currentStep: number;
  totalSteps: number;
  label?: string;
  className?: string;
}

export function ProgressIndicator({
  currentStep,
  totalSteps,
  label,
  className = '',
  ...props
}: ProgressIndicatorProps) {
  const progressPercent = Math.round((currentStep / totalSteps) * 100);
  const clampedPercent = Math.min(100, Math.max(0, progressPercent));

  return (
    <div className={`flex flex-col gap-2 ${className}`} {...props}>
      {/* Step label */}
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-text-secondary">
          {label || `Step ${currentStep} of ${totalSteps}`}
        </span>
        <span className="text-xs text-text-muted">{clampedPercent}%</span>
      </div>

      {/* Progress bar */}
      <div
        role="progressbar"
        aria-valuenow={currentStep}
        aria-valuemin={1}
        aria-valuemax={totalSteps}
        aria-label={label || `Step ${currentStep} of ${totalSteps}`}
        className="h-2 w-full rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden"
      >
        <div
          className="h-full rounded-full bg-primary transition-all duration-base"
          style={{ width: `${clampedPercent}%` }}
        />
      </div>

      {/* Step dots */}
      <div className="flex items-center justify-between" aria-hidden="true">
        {Array.from({ length: totalSteps }, (_, i) => {
          const step = i + 1;
          const isCompleted = step < currentStep;
          const isCurrent = step === currentStep;

          return (
            <div
              key={step}
              className={[
                'h-2.5 w-2.5 rounded-full transition-colors duration-base',
                isCompleted ? 'bg-primary' : '',
                isCurrent ? 'bg-primary ring-2 ring-primary/30' : '',
                !isCompleted && !isCurrent ? 'bg-[rgba(255,255,255,0.1)]' : '',
              ].join(' ')}
            />
          );
        })}
      </div>
    </div>
  );
}
