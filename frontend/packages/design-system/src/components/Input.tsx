import { type InputHTMLAttributes, useId } from 'react';

/**
 * Input component with label, error state, and tooltip trigger.
 * Supports aria-describedby for error messages and help text.
 *
 * Requirements: 18.3, 18.5, 25.7
 */

export interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label: string;
  error?: string;
  helpText?: string;
  className?: string;
}

export function Input({ ref, label, error, helpText, className = '', id: externalId, ...props }: InputProps & { ref?: React.Ref<HTMLInputElement> }) {
  const generatedId = useId();
  const inputId = externalId || generatedId;
  const errorId = `${inputId}-error`;
  const helpId = `${inputId}-help`;

  const describedBy = [
    error ? errorId : null,
    helpText ? helpId : null,
  ]
    .filter(Boolean)
    .join(' ') || undefined;

  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label
        htmlFor={inputId}
        className="text-sm font-medium text-text-secondary"
      >
        {label}
        {props.required && (
          <span className="text-error ml-1" aria-hidden="true">
            *
          </span>
        )}
      </label>

      <input
        ref={ref}
        id={inputId}
        aria-invalid={!!error}
        aria-describedby={describedBy}
        className={[
          'w-full px-3 py-2 rounded-md',
          'bg-surface border text-text-primary',
          'placeholder:text-text-muted',
          'transition-all duration-base',
          'outline-none',
          'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
          error
            ? 'border-error focus-visible:ring-error'
            : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.2)]',
          'disabled:opacity-50 disabled:cursor-not-allowed',
        ].join(' ')}
        {...props}
      />

      {helpText && !error && (
        <p id={helpId} className="text-xs text-text-muted">
          {helpText}
        </p>
      )}

      {error && (
        <p
          id={errorId}
          role="alert"
          className="text-xs text-error flex items-center gap-1"
        >
          <svg
            className="h-3 w-3 flex-shrink-0"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            aria-hidden="true"
          >
            <path
              fillRule="evenodd"
              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
              clipRule="evenodd"
            />
          </svg>
          {error}
        </p>
      )}
    </div>
  );
}
