import { useState, useEffect } from 'react';
import { ConfirmDialog } from '@guided-mentor/design-system';

/**
 * MaintenanceModePanel — Toggle platform maintenance mode.
 * Allows super admin to make the platform unavailable with estimated return time.
 *
 * Requirements: 31.5
 */

interface MaintenanceState {
  enabled: boolean;
  estimatedReturnTime: string | null;
  reason: string | null;
}

export function MaintenanceModePanel() {
  const [maintenanceState, setMaintenanceState] = useState<MaintenanceState>({
    enabled: false,
    estimatedReturnTime: null,
    reason: null,
  });
  const [loading, setLoading] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  // Form state for toggling
  const [newEnabled, setNewEnabled] = useState(false);
  const [estimatedReturn, setEstimatedReturn] = useState('');
  const [reason, setReason] = useState('');
  const [reasonError, setReasonError] = useState('');

  useEffect(() => {
    void fetchMaintenanceState();
  }, []);

  async function fetchMaintenanceState() {
    try {
      const response = await fetch('/v1/admin/dashboard', {
        headers: { 'Content-Type': 'application/json' },
      });
      if (response.ok) {
        const data = (await response.json()) as { maintenanceMode?: MaintenanceState };
        if (data.maintenanceMode) {
          setMaintenanceState(data.maintenanceMode);
        }
      }
    } catch {
      // Silently fail — panel will show default state
    }
  }

  function handleToggleClick() {
    const targetEnabled = !maintenanceState.enabled;
    setNewEnabled(targetEnabled);
    setEstimatedReturn('');
    setReason('');
    setReasonError('');
    setConfirmOpen(true);
  }

  function validateReason(value: string): boolean {
    if (value.length < 10) {
      setReasonError('Reason must be at least 10 characters');
      return false;
    }
    if (value.length > 500) {
      setReasonError('Reason must not exceed 500 characters');
      return false;
    }
    setReasonError('');
    return true;
  }

  async function handleConfirm() {
    if (!validateReason(reason)) return;

    try {
      setLoading(true);
      const response = await fetch('/v1/admin/maintenance', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          enabled: newEnabled,
          estimatedReturnTime: newEnabled ? estimatedReturn || null : null,
          reason: reason.trim(),
        }),
      });

      if (response.ok) {
        setMaintenanceState({
          enabled: newEnabled,
          estimatedReturnTime: newEnabled ? estimatedReturn || null : null,
          reason: reason.trim(),
        });
        setConfirmOpen(false);
      }
    } catch {
      // Error handling
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="glass-card rounded-xl p-6 space-y-4" data-testid="maintenance-mode-panel">
      <h2
        className="text-xl font-semibold"
        style={{ color: 'var(--color-text-primary)' }}
      >
        Maintenance Mode
      </h2>

      {/* Current Status Indicator */}
      <div className="flex items-center gap-3">
        <span
          className={`w-3 h-3 rounded-full ${
            maintenanceState.enabled ? 'bg-warning animate-pulse' : 'bg-success'
          }`}
          aria-hidden="true"
        />
        <span
          className="text-sm font-medium"
          style={{ color: 'var(--color-text-primary)' }}
          role="status"
          aria-live="polite"
        >
          {maintenanceState.enabled
            ? 'Platform is in MAINTENANCE'
            : 'Platform is LIVE'}
        </span>
      </div>

      {/* Show return time if in maintenance */}
      {maintenanceState.enabled && maintenanceState.estimatedReturnTime && (
        <p className="text-xs text-text-muted">
          Estimated return: {maintenanceState.estimatedReturnTime}
        </p>
      )}

      {maintenanceState.enabled && maintenanceState.reason && (
        <p className="text-xs text-text-muted">
          Reason: {maintenanceState.reason}
        </p>
      )}

      {/* Toggle Button */}
      <div className="flex items-center gap-4 pt-2">
        <button
          onClick={handleToggleClick}
          role="switch"
          aria-checked={maintenanceState.enabled}
          aria-label="Toggle maintenance mode"
          className={`relative inline-flex h-7 w-12 items-center rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none ${
            maintenanceState.enabled
              ? 'bg-warning'
              : 'bg-[rgba(255,255,255,0.1)]'
          }`}
        >
          <span
            className={`inline-block h-5 w-5 rounded-full bg-white transition-transform ${
              maintenanceState.enabled ? 'translate-x-6' : 'translate-x-1'
            }`}
            aria-hidden="true"
          />
        </button>
        <span
          className="text-sm"
          style={{ color: 'var(--color-text-secondary)' }}
        >
          {maintenanceState.enabled
            ? 'Click to bring platform back online'
            : 'Click to enable maintenance mode'}
        </span>
      </div>

      {/* Confirm Dialog */}
      <ConfirmDialog
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        onConfirm={() => void handleConfirm()}
        title={newEnabled ? 'Enable Maintenance Mode' : 'Disable Maintenance Mode'}
        description={
          <div className="space-y-3">
            <p>
              {newEnabled
                ? 'This will make the platform unavailable to all users. Only admins will retain access.'
                : 'This will restore the platform for all users.'}
            </p>

            {newEnabled && (
              <div className="flex flex-col gap-1">
                <label
                  htmlFor="maintenance-return-time"
                  className="text-xs font-medium text-text-secondary"
                >
                  Estimated Return Time (optional)
                </label>
                <input
                  id="maintenance-return-time"
                  type="text"
                  value={estimatedReturn}
                  onChange={(e) => setEstimatedReturn(e.target.value)}
                  placeholder="e.g., 2 hours, 6:00 PM AEST"
                  className="w-full px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary placeholder:text-text-muted outline-none focus-visible:ring-2 focus-visible:ring-primary"
                />
              </div>
            )}

            <div className="flex flex-col gap-1">
              <label
                htmlFor="maintenance-reason"
                className="text-xs font-medium text-text-secondary"
              >
                Reason (required, 10-500 characters)
              </label>
              <textarea
                id="maintenance-reason"
                value={reason}
                onChange={(e) => {
                  setReason(e.target.value);
                  if (reasonError) validateReason(e.target.value);
                }}
                placeholder="Enter reason for this action..."
                rows={3}
                className="w-full px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary placeholder:text-text-muted outline-none focus-visible:ring-2 focus-visible:ring-primary resize-none"
                required
                aria-required="true"
                aria-invalid={!!reasonError}
                aria-describedby={reasonError ? 'maintenance-reason-error' : undefined}
              />
              {reasonError && (
                <p id="maintenance-reason-error" className="text-xs text-error" role="alert">
                  {reasonError}
                </p>
              )}
            </div>
          </div>
        }
        confirmLabel={newEnabled ? 'Enable Maintenance Mode' : 'Bring Platform Online'}
        loading={loading}
        variant="warning"
      />
    </div>
  );
}

export default MaintenanceModePanel;
