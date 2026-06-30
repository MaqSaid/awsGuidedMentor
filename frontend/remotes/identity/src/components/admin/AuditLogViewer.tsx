import { useState, useEffect, useCallback } from 'react';

/**
 * AuditLogViewer — View admin audit log entries with filters.
 * Shows timestamped entries of all admin actions.
 *
 * Requirements: 31.8 (implied by 31.9 — audit log viewing)
 */

interface AuditLogEntry {
  id: string;
  adminId: string;
  adminName: string;
  action: string;
  target: string;
  reason: string;
  timestamp: string;
}

interface AuditLogFilters {
  startDate: string;
  endDate: string;
  actionType: string;
}

interface PaginatedAuditLog {
  entries: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const ACTION_TYPES = [
  'all',
  'disable_user',
  'enable_user',
  'toggle_feature',
  'maintenance_mode',
  'toggle_availability',
] as const;

const ACTION_LABELS: Record<string, string> = {
  all: 'All Actions',
  disable_user: 'Disable User',
  enable_user: 'Enable User',
  toggle_feature: 'Toggle Feature',
  maintenance_mode: 'Maintenance Mode',
  toggle_availability: 'Toggle Availability',
};

function formatRelativeTime(timestamp: string): string {
  const now = Date.now();
  const date = new Date(timestamp).getTime();
  const diffMs = now - date;

  const minutes = Math.floor(diffMs / 60000);
  const hours = Math.floor(diffMs / 3600000);
  const days = Math.floor(diffMs / 86400000);

  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes} minute${minutes !== 1 ? 's' : ''} ago`;
  if (hours < 24) return `${hours} hour${hours !== 1 ? 's' : ''} ago`;
  if (days < 7) return `${days} day${days !== 1 ? 's' : ''} ago`;
  return new Date(timestamp).toLocaleDateString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export function AuditLogViewer() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<AuditLogFilters>({
    startDate: '',
    endDate: '',
    actionType: 'all',
  });

  const pageSize = 20;
  const totalPages = Math.ceil(totalCount / pageSize);

  const fetchAuditLog = useCallback(async (fetchPage = 1) => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: fetchPage.toString(),
        pageSize: pageSize.toString(),
      });

      if (filters.startDate) params.set('startDate', filters.startDate);
      if (filters.endDate) params.set('endDate', filters.endDate);
      if (filters.actionType !== 'all') params.set('action', filters.actionType);

      const response = await fetch(`/v1/admin/audit-log?${params.toString()}`, {
        headers: { 'Content-Type': 'application/json' },
      });

      if (response.ok) {
        const data = (await response.json()) as PaginatedAuditLog;
        setEntries(data.entries);
        setTotalCount(data.totalCount);
        setPage(data.page);
      }
    } catch {
      // Error handling
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    void fetchAuditLog(1);
  }, [fetchAuditLog]);

  function handleFilter() {
    setPage(1);
    void fetchAuditLog(1);
  }

  function handlePageChange(newPage: number) {
    setPage(newPage);
    void fetchAuditLog(newPage);
  }

  return (
    <div className="glass-card rounded-xl p-6 space-y-4" data-testid="audit-log-viewer">
      <h2
        className="text-xl font-semibold"
        style={{ color: 'var(--color-text-primary)' }}
      >
        Audit Log
      </h2>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 items-end">
        <div className="flex flex-col gap-1">
          <label
            htmlFor="audit-start-date"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            From
          </label>
          <input
            id="audit-start-date"
            type="date"
            value={filters.startDate}
            onChange={(e) => setFilters((f) => ({ ...f, startDate: e.target.value }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label
            htmlFor="audit-end-date"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            To
          </label>
          <input
            id="audit-end-date"
            type="date"
            value={filters.endDate}
            onChange={(e) => setFilters((f) => ({ ...f, endDate: e.target.value }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label
            htmlFor="audit-action-type"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Action Type
          </label>
          <select
            id="audit-action-type"
            value={filters.actionType}
            onChange={(e) => setFilters((f) => ({ ...f, actionType: e.target.value }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            {ACTION_TYPES.map((type) => (
              <option key={type} value={type}>
                {ACTION_LABELS[type]}
              </option>
            ))}
          </select>
        </div>

        <button
          onClick={handleFilter}
          className="px-4 py-2 rounded-md text-sm font-medium text-[#0a1628] transition-all"
          style={{ background: 'var(--color-primary)' }}
          aria-label="Apply audit log filters"
        >
          Filter
        </button>
      </div>

      {/* Audit Log Table */}
      <div className="overflow-x-auto">
        <table
          className="w-full text-sm"
          role="table"
          aria-label="Audit log entries"
        >
          <thead>
            <tr
              className="border-b border-[rgba(255,255,255,0.08)]"
              style={{ color: 'var(--color-text-secondary)' }}
            >
              <th className="text-left py-3 px-2 font-medium">Timestamp</th>
              <th className="text-left py-3 px-2 font-medium">Admin</th>
              <th className="text-left py-3 px-2 font-medium">Action</th>
              <th className="text-left py-3 px-2 font-medium">Target</th>
              <th className="text-left py-3 px-2 font-medium">Reason</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={5} className="text-center py-8 text-text-muted">
                  Loading...
                </td>
              </tr>
            ) : entries.length === 0 ? (
              <tr>
                <td colSpan={5} className="text-center py-8 text-text-muted">
                  No audit log entries found.
                </td>
              </tr>
            ) : (
              entries.map((entry) => (
                <tr
                  key={entry.id}
                  className="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)]"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  <td
                    className="py-3 px-2 text-xs whitespace-nowrap"
                    title={new Date(entry.timestamp).toLocaleString('en-AU')}
                  >
                    {formatRelativeTime(entry.timestamp)}
                  </td>
                  <td className="py-3 px-2 text-text-secondary">{entry.adminName}</td>
                  <td className="py-3 px-2">
                    <span className="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-[rgba(255,255,255,0.05)] border border-[rgba(255,255,255,0.08)]">
                      {ACTION_LABELS[entry.action] ?? entry.action}
                    </span>
                  </td>
                  <td className="py-3 px-2 text-text-secondary text-xs">
                    {entry.target}
                  </td>
                  <td className="py-3 px-2 text-text-muted text-xs max-w-[200px] truncate">
                    {entry.reason}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <nav
          className="flex items-center justify-between pt-2"
          aria-label="Audit log pagination"
        >
          <p className="text-xs text-text-muted">
            Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of{' '}
            {totalCount} entries
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => handlePageChange(page - 1)}
              disabled={page <= 1}
              className="px-3 py-1 text-xs rounded border border-[rgba(255,255,255,0.08)] text-text-secondary hover:text-text-primary disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              aria-label="Previous page"
            >
              Previous
            </button>
            <span className="px-3 py-1 text-xs text-text-secondary">
              Page {page} of {totalPages}
            </span>
            <button
              onClick={() => handlePageChange(page + 1)}
              disabled={page >= totalPages}
              className="px-3 py-1 text-xs rounded border border-[rgba(255,255,255,0.08)] text-text-secondary hover:text-text-primary disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              aria-label="Next page"
            >
              Next
            </button>
          </div>
        </nav>
      )}
    </div>
  );
}

export default AuditLogViewer;
