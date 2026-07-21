import React, {
  useEffect,
  useRef,
  useCallback,
  type ReactNode,
  type HTMLAttributes,
} from 'react';

/**
 * Modal component with glassmorphism overlay, focus trap, and Escape to close.
 * Implements aria-modal, aria-labelledby for screen reader support.
 *
 * Requirements: 18.3, 18.5, 25.8
 */

export interface ModalProps extends HTMLAttributes<HTMLDivElement> {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  className?: string;
}

export function Modal({
  ref,
  open,
  onClose,
  title,
  children,
  className = '',
  ...props
}: ModalProps & { ref?: React.Ref<HTMLDivElement> }) {
  const modalRef = useRef<HTMLDivElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const titleId = `modal-title-${title.replace(/\s+/g, '-').toLowerCase()}`;

  // Focus trap: capture and restore focus
  useEffect(() => {
    if (!open) return;

    previousFocusRef.current = document.activeElement as HTMLElement;

    // Focus the modal itself on open
    const timer = setTimeout(() => {
      modalRef.current?.focus();
    }, 0);

    return () => {
      clearTimeout(timer);
      // Restore focus when closing
      previousFocusRef.current?.focus();
    };
  }, [open]);

  // Handle Escape key and Tab trap
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.stopPropagation();
        onClose();
        return;
      }

      // Focus trap
      if (e.key === 'Tab') {
        const modal = modalRef.current;
        if (!modal) return;

        const focusable = modal.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
        );

        if (focusable.length === 0) {
          e.preventDefault();
          return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last?.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first?.focus();
        }
      }
    },
    [onClose]
  );

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-modal-backdrop flex items-center justify-center p-4"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
      aria-hidden="true"
    >
      {/* Backdrop */}
      <div className="absolute inset-0 bg-[rgba(0,0,0,0.6)] backdrop-blur-sm" />

      {/* Modal panel */}
      <div
        ref={(node) => {
          (modalRef as React.MutableRefObject<HTMLDivElement | null>).current = node;
          if (typeof ref === 'function') ref(node);
          else if (ref && typeof ref === 'object') ref.current = node;
        }}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        onKeyDown={handleKeyDown}
        className={[
          'relative z-modal w-full max-w-lg',
          'glass-card p-6',
          'shadow-lg',
          'outline-none',
          className,
        ].join(' ')}
        {...props}
      >
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <h2
            id={titleId}
            className="text-xl font-semibold text-text-primary"
          >
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close modal"
            className="p-1 rounded-sm text-text-muted hover:text-text-primary transition-colors duration-fast focus-visible:ring-2 focus-visible:ring-primary outline-none"
          >
            <svg
              className="h-5 w-5"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path
                fillRule="evenodd"
                d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                clipRule="evenodd"
              />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div>{children}</div>
      </div>
    </div>
  );
}
