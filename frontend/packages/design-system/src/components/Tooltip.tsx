import { useState, useRef, useCallback, type ReactNode } from 'react';

/**
 * Tooltip — Contextual help tooltip triggered by hover or focus.
 * Used on form fields to explain expected input format and constraints.
 *
 * Requirements: 25.4
 */

export interface TooltipProps {
  /** Tooltip content */
  content: string;
  /** Element that triggers the tooltip (rendered as the child) */
  children: ReactNode;
  /** Position of the tooltip relative to the trigger */
  position?: 'top' | 'bottom' | 'left' | 'right';
  /** Additional className for the tooltip container */
  className?: string;
}

export function Tooltip({
  content,
  children,
  position = 'top',
  className = '',
}: TooltipProps) {
  const [isVisible, setIsVisible] = useState(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const show = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    setIsVisible(true);
  }, []);

  const hide = useCallback(() => {
    timeoutRef.current = setTimeout(() => {
      setIsVisible(false);
    }, 150);
  }, []);

  const positionClasses: Record<string, string> = {
    top: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
    bottom: 'top-full left-1/2 -translate-x-1/2 mt-2',
    left: 'right-full top-1/2 -translate-y-1/2 mr-2',
    right: 'left-full top-1/2 -translate-y-1/2 ml-2',
  };

  return (
    <div
      className={`relative inline-flex ${className}`}
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocus={show}
      onBlur={hide}
    >
      {children}
      {isVisible && content && (
        <div
          role="tooltip"
          className={[
            'absolute z-50 px-3 py-2 rounded-md',
            'bg-surface border border-[rgba(255,255,255,0.1)]',
            'text-xs text-text-secondary shadow-lg',
            'max-w-xs whitespace-normal',
            'pointer-events-none',
            'animate-in fade-in duration-150',
            positionClasses[position] || positionClasses.top,
          ].join(' ')}
        >
          {content}
        </div>
      )}
    </div>
  );
}
