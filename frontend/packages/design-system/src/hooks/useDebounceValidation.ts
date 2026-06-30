import { useState, useEffect, useRef, useCallback } from 'react';

/**
 * useDebounceValidation — Inline real-time validation with 300ms debounce.
 * Returns validation state (valid/invalid/pending) and error message as user types.
 * Displays validation tooltips explaining constraints.
 *
 * Requirements: 25.7
 */

export type ValidationState = 'idle' | 'validating' | 'valid' | 'invalid';

export interface ValidationRule {
  /** Validation function — return error message string on failure, or null on success */
  validate: (value: string) => string | null;
  /** Tooltip message explaining the constraint to the user */
  tooltip?: string;
}

export interface UseDebounceValidationOptions {
  /** Array of validation rules to apply in order */
  rules: ValidationRule[];
  /** Debounce delay in milliseconds (default: 300ms per Requirement 25.7) */
  debounceMs?: number;
  /** Whether to validate on mount (default: false) */
  validateOnMount?: boolean;
}

export interface UseDebounceValidationReturn {
  /** Current validation state */
  state: ValidationState;
  /** Error message from the first failing rule, or null if valid/idle */
  error: string | null;
  /** Tooltip text from the first rule that has one, for contextual help */
  tooltip: string | null;
  /** Whether the field has been touched (user has typed at least once) */
  touched: boolean;
  /** Call this when the field value changes */
  validate: (value: string) => void;
  /** Reset validation state back to idle */
  reset: () => void;
  /** Mark field as touched (e.g., on blur) */
  touch: () => void;
}

export function useDebounceValidation(
  options: UseDebounceValidationOptions
): UseDebounceValidationReturn {
  const { rules, debounceMs = 300, validateOnMount = false } = options;

  const [state, setState] = useState<ValidationState>('idle');
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const rulesRef = useRef(rules);
  rulesRef.current = rules;

  // Compute tooltip from first rule that has one
  const tooltip = rules.find((r) => r.tooltip)?.tooltip ?? null;

  const runValidation = useCallback((value: string) => {
    for (const rule of rulesRef.current) {
      const result = rule.validate(value);
      if (result !== null) {
        setState('invalid');
        setError(result);
        return;
      }
    }
    setState('valid');
    setError(null);
  }, []);

  const validate = useCallback(
    (value: string) => {
      setTouched(true);
      setState('validating');
      setError(null);

      // Clear existing timer
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }

      // Set new debounced validation
      timerRef.current = setTimeout(() => {
        runValidation(value);
      }, debounceMs);
    },
    [debounceMs, runValidation]
  );

  const reset = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
    }
    setState('idle');
    setError(null);
    setTouched(false);
  }, []);

  const touch = useCallback(() => {
    setTouched(true);
  }, []);

  // Cleanup timer on unmount
  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }
    };
  }, []);

  // Validate on mount if requested
  useEffect(() => {
    if (validateOnMount) {
      setState('validating');
    }
    // Only run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return { state, error, tooltip, touched, validate, reset, touch };
}
