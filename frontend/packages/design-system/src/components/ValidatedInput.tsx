import { useCallback, type ChangeEvent } from 'react';
import { Input, type InputProps } from './Input';
import { Tooltip } from './Tooltip';
import { useDebounceValidation, type ValidationRule } from '../hooks/useDebounceValidation';

/**
 * ValidatedInput — Input field with inline real-time validation (300ms debounce)
 * and contextual tooltip explaining constraints.
 *
 * Combines Input, Tooltip, and useDebounceValidation into a single ergonomic component.
 *
 * Requirements: 25.4, 25.7
 */

export interface ValidatedInputProps extends Omit<InputProps, 'error'> {
  /** Validation rules to apply with 300ms debounce */
  rules?: ValidationRule[];
  /** Override debounce delay (default: 300ms) */
  debounceMs?: number;
  /** Tooltip explaining input constraints (shown on hover/focus of info icon) */
  tooltip?: string;
  /** External error to display (e.g., from server validation) */
  externalError?: string;
  /** Callback with current validation state */
  onValidationChange?: (isValid: boolean) => void;
}

export function ValidatedInput({
  rules = [],
  debounceMs = 300,
  tooltip,
  externalError,
  onValidationChange,
  onChange,
  onBlur,
  ...inputProps
}: ValidatedInputProps) {
  const validation = useDebounceValidation({ rules, debounceMs });

  const handleChange = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const value = e.target.value;
      validation.validate(value);
      onChange?.(e);
    },
    [validation, onChange]
  );

  const handleBlur = useCallback(
    (e: React.FocusEvent<HTMLInputElement>) => {
      validation.touch();
      onBlur?.(e);
    },
    [validation, onBlur]
  );

  // Determine displayed error: external errors take priority, then validation
  const displayError = externalError || (validation.touched ? validation.error : null);

  // Determine validation indicator
  const validationIndicator =
    validation.state === 'validating' ? (
      <span className="text-xs text-text-muted animate-pulse">Checking...</span>
    ) : validation.state === 'valid' && validation.touched && !externalError ? (
      <svg
        className="h-4 w-4 text-success"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 20 20"
        fill="currentColor"
        aria-label="Valid"
      >
        <path
          fillRule="evenodd"
          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
          clipRule="evenodd"
        />
      </svg>
    ) : null;

  // Notify parent of validation state changes
  if (onValidationChange) {
    const isValid = validation.state === 'valid' && !externalError;
    // Defer to avoid state update during render
    setTimeout(() => onValidationChange(isValid), 0);
  }

  return (
    <div className="relative">
      <div className="flex items-center gap-2">
        <div className="flex-1">
          <Input
            {...inputProps}
            error={displayError || undefined}
            onChange={handleChange}
            onBlur={handleBlur}
          />
        </div>

        {/* Validation state indicator */}
        {validationIndicator && (
          <div className="flex-shrink-0 mt-6">{validationIndicator}</div>
        )}

        {/* Tooltip info icon */}
        {tooltip && (
          <div className="flex-shrink-0 mt-6">
            <Tooltip content={tooltip} position="top">
              <button
                type="button"
                className="text-text-muted hover:text-text-secondary transition-colors p-1 rounded-full focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background"
                aria-label={`Help: ${inputProps.label}`}
                tabIndex={0}
              >
                <svg
                  className="h-4 w-4"
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  aria-hidden="true"
                >
                  <path
                    fillRule="evenodd"
                    d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z"
                    clipRule="evenodd"
                  />
                </svg>
              </button>
            </Tooltip>
          </div>
        )}
      </div>
    </div>
  );
}
