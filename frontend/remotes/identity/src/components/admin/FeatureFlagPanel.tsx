import { useState, useEffect } from 'react';
import { ConfirmDialog } from '@guided-mentor/design-system';

/**
 * FeatureFlagPanel — Toggle platform feature flags.
 * Allows super admin to enable/disable features without deployment.
 *
 * Requirements: 31.6
 */

interface FeatureFlag {
  name: string;
  displayName: string;
  enabled: boolean;
  description: string;
}

const DEFAULT_FEATURES: FeatureFlag[] = [
  {
    name: 'ai-help-assistant',
    displayName: 'AI Help Assistant',
    enabled: true,
    description: 'Floating AI chat for contextual platform help',
  },
  {
    name: 'job-board',
    displayName: 'Job Board',
    enabled: true,
    description: 'Opportunities board for mentors to post jobs and events',
  },
  {
    name: 'meetup-calendar',
    displayName: 'Meetup Calendar',
    enabled: true,
    description: 'AWS User Group meetup event calendar',
  },
  {
    name: 'session-plans',
    displayName: 'Session Plans',
    enabled: true,
    description: 'AI-generated session plan creation via Bedrock',
  },
];

export function FeatureFlagPanel() {
  const [features, setFeatures] = useState<FeatureFlag[]>(DEFAULT_FEATURES);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [targetFeature, setTargetFeature] = useState<FeatureFlag | null>(null);
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    void fetchFeatureFlags();
  }, []);

  async function fetchFeatureFlags() {
    try {
      const response = await fetch('/v1/admin/dashboard', {
        headers: { 'Content-Type': 'application/json' },
      });
      if (response.ok) {
        const data = (await response.json()) as { featureFlags?: FeatureFlag[] };
        if (data.featureFlags && data.featureFlags.length > 0) {
          setFeatures(data.featureFlags);
        }
      }
    } catch {
      // Use defaults on error
    }
  }

  function handleToggleClick(feature: FeatureFlag) {
    setTargetFeature(feature);
    setReason('');
    setConfirmOpen(true);
  }

  async function handleConfirm() {
    if (!targetFeature || !reason.trim()) return;

    try {
      setLoading(true);
      const newEnabled = !targetFeature.enabled;
      const response = await fetch(`/v1/admin/features/${targetFeature.name}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ enabled: newEnabled, reason: reason.trim() }),
      });

      if (response.ok) {
        setFeatures((prev) =>
          prev.map((f) =>
            f.name === targetFeature.name ? { ...f, enabled: newEnabled } : f
          )
        );
        setConfirmOpen(false);
      }
    } catch {
      // Error handling
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="glass-card rounded-xl p-6 space-y-4" data-testid="feature-flag-panel">
      <h2
        className="text-xl font-semibold"
        style={{ color: 'var(--color-text-primary)' }}
      >
        Feature Flags
      </h2>

      <p className="text-xs text-text-muted">
        Enable or disable platform features without deployment.
      </p>

      {/* Feature List */}
      <ul className="space-y-3" role="list" aria-label="Feature flags">
        {features.map((feature) => (
          <li
            key={feature.name}
            className="flex items-center justify-between p-3 rounded-lg bg-[rgba(255,255,255,0.02)] border border-[rgba(255,255,255,0.04)]"
          >
            <div className="flex items-center gap-3">
              {/* Status dot */}
              <span
                className={`w-2.5 h-2.5 rounded-full flex-shrink-0 ${
                  feature.enabled ? 'bg-success' : 'bg-error'
                }`}
                aria-hidden="true"
              />
              <div>
                <p
                  className="text-sm font-medium"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  {feature.displayName}
                </p>
                <p className="text-xs text-text-muted">{feature.description}</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <span className="text-xs text-text-muted">
                {feature.enabled ? 'Enabled' : 'Disabled'}
              </span>
              <button
                onClick={() => handleToggleClick(feature)}
                role="switch"
                aria-checked={feature.enabled}
                aria-label={`Toggle ${feature.displayName}: currently ${feature.enabled ? 'enabled' : 'disabled'}`}
                className={`relative inline-flex h-6 w-10 items-center rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background outline-none ${
                  feature.enabled
                    ? 'bg-success'
                    : 'bg-[rgba(255,255,255,0.1)]'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 rounded-full bg-white transition-transform ${
                    feature.enabled ? 'translate-x-5' : 'translate-x-1'
                  }`}
                  aria-hidden="true"
                />
              </button>
            </div>
          </li>
        ))}
      </ul>

      {/* Confirm Dialog */}
      <ConfirmDialog
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        onConfirm={() => void handleConfirm()}
        title={
          targetFeature
            ? `${targetFeature.enabled ? 'Disable' : 'Enable'} ${targetFeature.displayName}`
            : 'Toggle Feature'
        }
        description={
          <div className="space-y-3">
            <p>
              {targetFeature?.enabled
                ? `Are you sure you want to disable "${targetFeature.displayName}"? Users will lose access to this feature immediately.`
                : `Are you sure you want to enable "${targetFeature?.displayName}"? This will make the feature available to all users.`}
            </p>
            <div className="flex flex-col gap-1">
              <label
                htmlFor="feature-reason"
                className="text-xs font-medium text-text-secondary"
              >
                Reason (required)
              </label>
              <textarea
                id="feature-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="Enter reason for this change..."
                rows={2}
                className="w-full px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary placeholder:text-text-muted outline-none focus-visible:ring-2 focus-visible:ring-primary resize-none"
                required
                aria-required="true"
              />
            </div>
          </div>
        }
        confirmLabel={targetFeature?.enabled ? 'Disable Feature' : 'Enable Feature'}
        loading={loading}
        variant="warning"
      />
    </div>
  );
}

export default FeatureFlagPanel;
