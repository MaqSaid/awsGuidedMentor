import { type ButtonHTMLAttributes, type ReactNode, type Ref } from 'react';

/**
 * Button component with primary/secondary/ghost variants, loading and disabled states.
 * Supports ARIA labels and keyboard navigation.
 *
 * Requirements: 18.3, 18.5
 */

export type ButtonVariant = 'primary' | 'secondary' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  children: ReactNode;
  className?: string;
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: [
    'bg-primary text-[#0a1628] font-semibold',
    'hover:opacity-90 hover:shadow-glow-orange',
    'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
    'active:scale-[0.98]',
  ].join(' '),
  secondary: [
    'bg-transparent text-primary border border-primary',
    'hover:bg-primary/10 hover:shadow-glow-orange',
    'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
    'active:scale-[0.98]',
  ].join(' '),
  ghost: [
    'bg-transparent text-text-secondary',
    'hover:text-text-primary hover:bg-[rgba(255,255,255,0.05)]',
    'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
    'active:scale-[0.98]',
  ].join(' '),
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'px-3 py-1.5 text-sm rounded-sm',
  md: 'px-4 py-2 text-base rounded-md',
  lg: 'px-6 py-3 text-lg rounded-md',
};

export function Button({
  ref,
  variant = 'primary',
  size = 'md',
  loading = false,
  disabled,
  children,
  className = '',
  ...props
}: ButtonProps & { ref?: Ref<HTMLButtonElement> }) {
  const isDisabled = disabled || loading;

  return (
    <button
      ref={ref}
      disabled={isDisabled}
      aria-busy={loading}
      aria-disabled={isDisabled}
      className={[
        'inline-flex items-center justify-center gap-2',
        'font-medium transition-all duration-base',
        'outline-none cursor-pointer select-none',
        'disabled:opacity-50 disabled:cursor-not-allowed disabled:pointer-events-none',
        variantClasses[variant],
        sizeClasses[size],
        className,
      ].join(' ')}
      {...props}
    >
      {loading && (
        <svg
          className="animate-spin h-4 w-4"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle
            className="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            strokeWidth="4"
          />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
          />
        </svg>
      )}
      {children}
    </button>
  );
}
