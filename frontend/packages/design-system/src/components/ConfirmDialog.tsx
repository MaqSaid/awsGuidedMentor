import { useRef, useEffect, type ReactNode } from 'react';
import { Button } from './Button';
import { Modal } from './Modal';

/**
 * ConfirmDialog component for destructive action confirmations.
 * Displays consequence description with Cancel/Confirm buttons.
 * Auto-focuses Cancel button (safest option).
 *
 * Requirements: 18.3, 25.8
 */

export interface ConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  description: string | ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  loading?: boolean;
  variant?: 'danger' | 'warning';
  className?: string;
}

export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  loading = false,
  variant = 'danger',
  className = '',
}: ConfirmDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);

  // Focus the Cancel button on open (safest default action)
  useEffect(() => {
    if (open) {
      const timer = setTimeout(() => cancelRef.current?.focus(), 50);
      return () => clearTimeout(timer);
    }
  }, [open]);

  return (
    <Modal open={open} onClose={onClose} title={title} className={className}>
      <div className="space-y-4">
        <div className="text-sm text-text-secondary">
          {typeof description === 'string' ? <p>{description}</p> : description}
        </div>

        <div className="flex items-center justify-end gap-3 pt-2">
          <Button
            ref={cancelRef}
            variant="ghost"
            onClick={onClose}
            disabled={loading}
          >
            {cancelLabel}
          </Button>
          <Button
            variant="primary"
            onClick={onConfirm}
            loading={loading}
            className={
              variant === 'danger'
                ? 'bg-error hover:bg-error/90 hover:shadow-none'
                : 'bg-warning hover:bg-warning/90 hover:shadow-none'
            }
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
