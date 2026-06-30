/**
 * AvailabilityToggle — One-click Available/Unavailable toggle
 * with reason + return date inputs for unavailability.
 *
 * Requirements: 32.1, 32.3, 32.6, 32.8
 */
import { useState, useCallback } from 'react';
import { Button, Input } from '@guided-mentor/design-system';
import { useMentorAvailability, useSetMentorAvailability } from '../api/mentoring-api';
import type { AvailabilityStatus } from '../types';

interface AvailabilityToggleProps {
  className?: string;
}

export function AvailabilityToggle({ className = '' }: AvailabilityToggleProps) {
  const { data: availability, isLoading } = useMentorAvailability();
  const setAvailability = useSetMentorAvailability();

  const [showUnavailableForm, setShowUnavailableForm] = useState(false);
  const [reason, setReason] = useState('');
  const [returnDate, setReturnDate] = useState('');

  const isAvailable = availability?.status === 'available';

  const handleToggle = useCallback(() => {
    if (isAvailable) {
      // Going unavailable: show form for reason/return date
      setShowUnavailableForm(true);
    } else {
      // Going available: immediate toggle
      setAvailability.mutate({
        status: 'available' as AvailabilityStatus,
        reason: undefined,
        returnDate: undefined,
      });
      setShowUnavailableForm(false);
    }
  }, [isAvailable, setAvailability]);

  const handleConfirmUnavailable = useCallback(() => {
    setAvailability.mutate(
      {
        status: 'unavailable' as AvailabilityStatus,
        reason: reason.trim() || undefined,
        returnDate: returnDate || undefined,
      },
      {
        onSuccess: () => {
          setShowUnavailableForm(false);
          setReason('');
          setReturnDate('');
        },
      }
    );
  }, [reason, returnDate, setAvailability]);

  const handleCancelUnavailable = useCallback(() => {
    setShowUnavailableForm(false);
    setReason('');
    setReturnDate('');
  }, []);

  if (isLoading) {
    return (
      <div className={['glass-card p-4 rounded-lg animate-pulse', className].join(' ')}>
        <div className="h-6 bg-surface rounded w-1/2" />
      </div>
    );
  }

  return (
    <div className={['glass-card p-4 rounded-lg space-y-3', className].join(' ')}>
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-text-primary">Availability Status</p>
          <p className="text-xs text-text-muted">
            {isAvailable
              ? 'You are visible to mentees in Browse'
              : 'You are hidden from Browse'}
          </p>
          {!isAvailable && availability?.reason && (
            <p className="text-xs text-text-muted mt-0.5">
              Reason: {availability.reason}
            </p>
          )}
          {!isAvailable && availability?.returnDate && (
            <p className="text-xs text-warning mt-0.5">
              Back: {new Date(availability.returnDate).toLocaleDateString('en-AU', {
                day: 'numeric',
                month: 'short',
                year: 'numeric',
              })}
            </p>
          )}
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={isAvailable}
          aria-label={`Toggle availability. Currently ${isAvailable ? 'available' : 'unavailable'}`}
          onClick={handleToggle}
          disabled={setAvailability.isPending}
          className={[
            'relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-base',
            'focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none',
            isAvailable ? 'bg-success' : 'bg-[rgba(255,255,255,0.1)]',
            setAvailability.isPending ? 'opacity-50 cursor-not-allowed' : '',
          ].join(' ')}
        >
          <span
            className={[
              'inline-block h-4 w-4 rounded-full bg-white transition-transform duration-base',
              isAvailable ? 'translate-x-6' : 'translate-x-1',
            ].join(' ')}
          />
        </button>
      </div>

      {/* Unavailability reason/return date form */}
      {showUnavailableForm && (
        <div className="space-y-3 pt-2 border-t border-[rgba(255,255,255,0.08)]">
          <p className="text-xs text-text-secondary">
            Let your mentees know why you're taking a break (optional).
          </p>
          <Input
            label="Reason (optional)"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="e.g. Vacation, personal commitments"
          />
          <Input
            label="Expected return date (optional)"
            type="date"
            value={returnDate}
            onChange={(e) => setReturnDate(e.target.value)}
          />
          <div className="flex gap-2 justify-end">
            <Button variant="ghost" size="sm" onClick={handleCancelUnavailable}>
              Cancel
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={handleConfirmUnavailable}
              loading={setAvailability.isPending}
            >
              Go Unavailable
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
