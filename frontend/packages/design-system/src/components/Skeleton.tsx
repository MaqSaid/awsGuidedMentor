import type { HTMLAttributes } from 'react';

/**
 * Skeleton loading placeholder component.
 * Renders a pulsing rectangle with configurable width/height.
 * Respects prefers-reduced-motion (pulse animation disabled via glass.css).
 *
 * Requirements: 18.3, 25.10
 */

export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {
  width?: string | number;
  height?: string | number;
  rounded?: 'sm' | 'md' | 'lg' | 'full';
  className?: string;
}

export function Skeleton({
  width,
  height = '1rem',
  rounded = 'md',
  className = '',
  ...props
}: SkeletonProps) {
  const roundedClasses: Record<string, string> = {
    sm: 'rounded-sm',
    md: 'rounded-md',
    lg: 'rounded-lg',
    full: 'rounded-full',
  };

  return (
    <div
      role="status"
      aria-label="Loading"
      aria-busy="true"
      style={{ width, height }}
      className={[
        'animate-pulse',
        'bg-[rgba(255,255,255,0.06)]',
        roundedClasses[rounded],
        className,
      ].join(' ')}
      {...props}
    />
  );
}
