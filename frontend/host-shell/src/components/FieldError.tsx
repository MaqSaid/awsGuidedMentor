interface FieldErrorProps {
  name: string;
  error: string | null;
}

/**
 * FieldError — inline error message for form field validation.
 * Displays with role="alert" and is linked via aria-describedby.
 * Requirements: 25.7
 */
export function FieldError({ name, error }: FieldErrorProps) {
  if (!error) return null;

  return (
    <p
      id={`${name}-error`}
      role="alert"
      className="mt-1 text-xs text-rose animate-[fadeIn_150ms_ease-in]"
    >
      {error}
    </p>
  );
}
