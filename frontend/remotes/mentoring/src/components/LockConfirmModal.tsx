/**
 * LockConfirmModal — 15-min timer display with confirm/cancel buttons.
 * Uses the design system Modal for glassmorphism + focus trap.
 *
 * Requirements: 6.2, 6.4, 6.5, 6.7
 */
import { useState, useEffect, useCallback } from 'react';
import { Modal, Button } from '@guided-mentor/design-system';

interface LockConfirmModalProps {
  open: boolean;
  mentorName: string;
  expiresAt: string;
  onConfirm: () => void;
  onCancel: () => void;
  confirmLoading?: boolean;
  cancelLoading?: boolean;
}

function formatTimeRemaining(seconds: number): string {
  if (seconds <= 0) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export function LockConfirmModal({
  open,
  mentorName,
  expiresAt,
  onConfirm,
  onCancel,
  confirmLoading = false,
  cancelLoading = false,
}: LockConfirmModalProps) {
  const [secondsRemaining, setSecondsRemaining] = useState(0);

  const computeRemaining = useCallback(() => {
    const diff = Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000));
    return diff;
  }, [expiresAt]);

  useEffect(() => {
    if (!open) return;
    setSecondsRemaining(computeRemaining());

    const interval = setInterval(() => {
      const remaining = computeRemaining();
      setSecondsRemaining(remaining);
      if (remaining <= 0) {
        clearInterval(interval);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [open, computeRemaining]);

  const isExpired = secondsRemaining <= 0;
  const isUrgent = secondsRemaining <= 120 && secondsRemaining > 0;

  return (
    <Modal open={open} onClose={onCancel} title="Confirm Mentor Selection">
      <div className="space-y-4">
        <p className="text-text-secondary">
          You have placed a hold on <span className="text-text-primary font-medium">{mentorName}</span>.
          Please confirm your selection or cancel within the time limit.
        </p>

        {/* Timer */}
        <div
          className={[
            'flex items-center justify-center py-3 rounded-md border',
            isExpired
              ? 'bg-error/10 border-error/30 text-error'
              : isUrgent
                ? 'bg-warning/10 border-warning/30 text-warning'
                : 'bg-surface border-white/10 text-text-primary',
          ].join(' ')}
          role="timer"
          aria-live="polite"
          aria-label={`Time remaining: ${formatTimeRemaining(secondsRemaining)}`}
        >
          <span className="text-2xl font-mono font-semibold">
            {isExpired ? 'Expired' : formatTimeRemaining(secondsRemaining)}
          </span>
        </div>

        {isExpired && (
          <p className="text-error text-sm text-center">
            The hold has expired. The mentor is now available to other mentees.
          </p>
        )}

        {/* Action buttons */}
        <div className="flex gap-3 justify-end pt-2">
          <Button
            variant="ghost"
            onClick={onCancel}
            loading={cancelLoading}
            disabled={confirmLoading}
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={onConfirm}
            loading={confirmLoading}
            disabled={isExpired || cancelLoading}
          >
            Confirm Selection
          </Button>
        </div>
      </div>
    </Modal>
  );
}
