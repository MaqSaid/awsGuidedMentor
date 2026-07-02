import { useState, useEffect } from 'react';
import { UserSearchPanel } from '../components/admin/UserSearchPanel';
import { MaintenanceModePanel } from '../components/admin/MaintenanceModePanel';
import { FeatureFlagPanel } from '../components/admin/FeatureFlagPanel';
import { AuditLogViewer } from '../components/admin/AuditLogViewer';

/**
 * AdminDashboardPage — Super Admin dashboard
 * Displays platform stats, health, user management, audit log, feature flags, and maintenance mode.
 *
 * Requirements: 31.2, 31.3, 31.4, 31.5, 31.6, 31.8
 */

interface PlatformStats {
  totalUsers: number;
  mentors: number;
  mentees: number;
  activeSessions: number;
}

type HealthStatus = 'healthy' | 'degraded' | 'critical';

interface AdminDashboardData {
  stats: PlatformStats;
  healthStatus: HealthStatus;
  alarmStates: { name: string; state: 'OK' | 'ALARM' | 'INSUFFICIENT_DATA' }[];
}

const healthBadgeClasses: Record<HealthStatus, string> = {
  healthy: 'bg-success/20 text-success border-success/30',
  degraded: 'bg-warning/20 text-warning border-warning/30',
  critical: 'bg-error/20 text-error border-error/30',
};

const healthLabels: Record<HealthStatus, string> = {
  healthy: 'All Systems Operational',
  degraded: 'Degraded Performance',
  critical: 'Critical Issues Detected',
};

export function AdminDashboardPage() {
  const [dashboardData, setDashboardData] = useState<AdminDashboardData>({
    stats: { totalUsers: 0, mentors: 0, mentees: 0, activeSessions: 0 },
    healthStatus: 'healthy',
    alarmStates: [],
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  async function fetchDashboardData() {
    try {
      setLoading(true);
      const response = await fetch('/v1/admin/dashboard', {
        headers: { 'Content-Type': 'application/json' },
      });
      if (response.ok) {
        const data = (await response.json()) as AdminDashboardData;
        setDashboardData(data);
      }
    } catch {
      // Dashboard will show zeros on error
    } finally {
      setLoading(false);
    }
  }

  const { stats, healthStatus } = dashboardData;

  const statCards: { label: string; value: number; icon: string }[] = [
    { label: 'Total Users', value: stats.totalUsers, icon: '👥' },
    { label: 'Mentors', value: stats.mentors, icon: '🎓' },
    { label: 'Mentees', value: stats.mentees, icon: '📚' },
    { label: 'Active Sessions', value: stats.activeSessions, icon: '🔗' },
  ];

  return (
    <div
      data-testid="admin-dashboard-page"
      className="min-h-screen px-4 md:px-8 py-6 md:py-8 space-y-6 md:space-y-8"
      style={{ background: 'var(--color-background)' }}
    >
      {/* Header */}
      <header className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1
            className="text-3xl font-bold"
            style={{ color: 'var(--color-text-primary)' }}
          >
            Admin Dashboard
          </h1>
          <p
            className="text-sm mt-1"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Platform management and monitoring
          </p>
        </div>

        {/* Platform Health Badge */}
        <div
          className={`inline-flex items-center gap-2 px-4 py-2 rounded-full border text-sm font-medium ${healthBadgeClasses[healthStatus]}`}
          role="status"
          aria-label={`Platform health: ${healthLabels[healthStatus]}`}
        >
          <span
            className={`w-2 h-2 rounded-full ${
              healthStatus === 'healthy'
                ? 'bg-success'
                : healthStatus === 'degraded'
                  ? 'bg-warning'
                  : 'bg-error'
            }`}
            aria-hidden="true"
          />
          {healthLabels[healthStatus]}
        </div>
      </header>

      {/* Stats Cards */}
      <section aria-label="Platform statistics">
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 md:gap-6">
          {statCards.map((card) => (
            <div
              key={card.label}
              className="glass-card p-4 md:p-6 rounded-xl"
              role="group"
              aria-label={card.label}
            >
              <div className="flex items-center justify-between">
                <div>
                  <p
                    className="text-sm font-medium"
                    style={{ color: 'var(--color-text-secondary)' }}
                  >
                    {card.label}
                  </p>
                  <p
                    className="text-3xl font-bold mt-1"
                    style={{ color: 'var(--color-text-primary)' }}
                  >
                    {loading ? '—' : card.value.toLocaleString()}
                  </p>
                </div>
                <span className="text-3xl" aria-hidden="true">
                  {card.icon}
                </span>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Main Grid: User Management + Audit Log */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 md:gap-8">
        {/* User Management Panel */}
        <section aria-label="User management">
          <UserSearchPanel />
        </section>

        {/* Audit Log Viewer */}
        <section aria-label="Audit log">
          <AuditLogViewer />
        </section>
      </div>

      {/* Secondary Grid: Feature Flags + Maintenance Mode */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 md:gap-8">
        {/* Feature Flags Panel */}
        <section aria-label="Feature flags">
          <FeatureFlagPanel />
        </section>

        {/* Maintenance Mode Panel */}
        <section aria-label="Maintenance mode">
          <MaintenanceModePanel />
        </section>
      </div>
    </div>
  );
}

export default AdminDashboardPage;
