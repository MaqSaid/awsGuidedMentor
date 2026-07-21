import { useCallback, useRef, useState } from 'react';

type ValidationRule = (value: string) => string | null;

interface FieldState {
  value: string;
  error: string | null;
  touched: boolean;
  isValid: boolean;
}

interface UseInlineValidationReturn {
  fields: Record<string, FieldState>;
  register: (name: string, rules: ValidationRule[]) => {
    value: string;
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => void;
    onBlur: () => void;
    'aria-invalid': boolean;
    'aria-describedby': string;
  };
  getError: (name: string) => string | null;
  isFormValid: () => boolean;
  validateAll: () => boolean;
  reset: () => void;
}

/**
 * useInlineValidation — hook for real-time inline validation with 300ms debounce.
 * Validates as the user types, showing validation state (valid/invalid) with tooltips.
 * Requirements: 25.7
 */
export function useInlineValidation(): UseInlineValidationReturn {
  const [fields, setFields] = useState<Record<string, FieldState>>({});
  const rulesRef = useRef<Record<string, ValidationRule[]>>({});
  const debounceTimers = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

  const validate = useCallback((name: string, value: string): string | null => {
    const rules = rulesRef.current[name] ?? [];
    for (const rule of rules) {
      const error = rule(value);
      if (error) return error;
    }
    return null;
  }, []);

  const updateField = useCallback((name: string, value: string, touched: boolean) => {
    // Clear existing debounce timer
    if (debounceTimers.current[name]) {
      clearTimeout(debounceTimers.current[name]);
    }

    // Update value immediately
    setFields((prev) => ({
      ...prev,
      [name]: { ...prev[name]!, value, touched, error: prev[name]?.error ?? null, isValid: prev[name]?.isValid ?? false },
    }));

    // Debounce validation by 300ms
    debounceTimers.current[name] = setTimeout(() => {
      const error = validate(name, value);
      setFields((prev) => ({
        ...prev,
        [name]: { value, error, touched, isValid: error === null && value.length > 0 },
      }));
    }, 300);
  }, [validate]);

  const register = useCallback((name: string, rules: ValidationRule[]) => {
    rulesRef.current[name] = rules;

    // Initialize field if not present
    if (!fields[name]) {
      setFields((prev) => ({
        ...prev,
        [name]: { value: '', error: null, touched: false, isValid: false },
      }));
    }

    const field = fields[name] ?? { value: '', error: null, touched: false, isValid: false };

    return {
      value: field.value,
      onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
        updateField(name, e.target.value, true);
      },
      onBlur: () => {
        // Validate immediately on blur
        const error = validate(name, field.value);
        setFields((prev) => ({
          ...prev,
          [name]: { ...prev[name]!, touched: true, error, isValid: error === null && field.value.length > 0 },
        }));
      },
      'aria-invalid': field.touched && field.error !== null,
      'aria-describedby': `${name}-error`,
    };
  }, [fields, updateField, validate]);

  const getError = useCallback((name: string): string | null => {
    const field = fields[name];
    if (!field?.touched) return null;
    return field.error;
  }, [fields]);

  const isFormValid = useCallback((): boolean => {
    return Object.values(fields).every((f) => f.isValid);
  }, [fields]);

  const validateAll = useCallback((): boolean => {
    let allValid = true;
    const updated = { ...fields };

    for (const name of Object.keys(rulesRef.current)) {
      const value = fields[name]?.value ?? '';
      const error = validate(name, value);
      updated[name] = { value, error, touched: true, isValid: error === null && value.length > 0 };
      if (error !== null) allValid = false;
    }

    setFields(updated);
    return allValid;
  }, [fields, validate]);

  const reset = useCallback(() => {
    setFields({});
    Object.values(debounceTimers.current).forEach(clearTimeout);
    debounceTimers.current = {};
  }, []);

  return { fields, register, getError, isFormValid, validateAll, reset };
}

// Common validation rules
export const validationRules = {
  required: (label: string): ValidationRule => (value) =>
    value.trim().length === 0 ? `${label} is required` : null,

  minLength: (min: number, label: string): ValidationRule => (value) =>
    value.length > 0 && value.length < min ? `${label} must be at least ${min} characters` : null,

  maxLength: (max: number, label: string): ValidationRule => (value) =>
    value.length > max ? `${label} must be at most ${max} characters` : null,

  email: (): ValidationRule => (value) =>
    value.length > 0 && !value.includes('@') ? 'Please enter a valid email address' : null,

  pattern: (regex: RegExp, message: string): ValidationRule => (value) =>
    value.length > 0 && !regex.test(value) ? message : null,
};
