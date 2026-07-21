import { useRef, useEffect, useState } from 'react';
import { Modal } from './Modal';

interface ConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void | Promise<void>;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'warning';
}

/**
 * ConfirmDialog — confirmation modal for destructive actions.
 * Displays clear consequence description with cancel option.
 * Requirements: 25.8
 */
export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'danger',
}: ConfirmDialogProps) {
  const [isPending, setIsPending] = useState(false);
  const confirmRef = useRef<HTMLButtonElement>(null);

  // Focus confirm button on open for keyboard accessibility
  useEffect(() => {
    if (open) {
      setTimeout(() => confirmRef.current?.focus(), 100);
    }
  }, [open]);

  async function handleConfirm() {
    setIsPending(true);
    try {
      await onConfirm();
    } finally {
      setIsPending(false);
      onClose();
    }
  }

  const confirmBtnClass = variant === 'danger'
    ? 'bg-rose text-white hover:bg-rose/80'
    : 'bg-amber text-bg-primary hover:bg-amber/80';

  return (
    <Modal open={open} onClose={onClose} title={title}>
      <p className="text-sm text-text-secondary mb-6">{description}</p>
      <div className="flex items-center justify-end gap-3">
        <button
          onClick={onClose}
          className="btn-ghost text-sm px-4 py-2"
          disabled={isPending}
          aria-label={cancelLabel}
        >
          {cancelLabel}
        </button>
        <button
          ref={confirmRef}
          onClick={handleConfirm}
          disabled={isPending}
          className={`px-4 py-2 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${confirmBtnClass}`}
          aria-label={confirmLabel}
        >
          {isPending ? (
            <span className="flex items-center gap-2">
              <span className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" aria-hidden="true" />
              Processing...
            </span>
          ) : (
            confirmLabel
          )}
        </button>
      </div>
    </Modal>
  );
}
