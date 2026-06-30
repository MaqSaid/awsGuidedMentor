import { useState, useCallback } from 'react';
import { ConfirmDialog } from '@guided-mentor/design-system';

/**
 * UserSearchPanel — Admin user search, filter, and management panel.
 * Allows super admin to search/filter users and disable/enable accounts.
 *
 * Requirements: 31.3, 31.4
 */

type UserRole = 'all' | 'mentor' | 'mentee';
type UserStatus = 'all' | 'active' | 'disabled' | 'locked';

type AustralianChapter =
  | 'all'
  | 'Sydney'
  | 'Melbourne'
  | 'Brisbane'
  | 'Perth'
  | 'Adelaide'
  | 'Canberra'
  | 'Hobart'
  | 'Darwin'
  | 'GoldCoast'
  | 'Newcastle'
  | 'Wollongong'
  | 'Geelong'
  | 'Townsville';

const CHAPTERS: AustralianChapter[] = [
  'all',
  'Sydney',
  'Melbourne',
  'Brisbane',
  'Perth',
  'Adelaide',
  'Canberra',
  'Hobart',
  'Darwin',
  'GoldCoast',
  'Newcastle',
  'Wollongong',
  'Geelong',
  'Townsville',
];

interface UserSearchFilters {
  name: string;
  email: string;
  role: UserRole;
  chapter: AustralianChapter;
  status: UserStatus;
}

interface AdminUser {
  id: string;
  displayName: string;
  email: string;
  activeRole: string | null;
  awsChapter: string;
  status: 'active' | 'disabled' | 'locked';
}

interface PaginatedResponse {
  users: AdminUser[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export function UserSearchPanel() {
  const [filters, setFilters] = useState<UserSearchFilters>({
    name: '',
    email: '',
    role: 'all',
    chapter: 'all',
    status: 'all',
  });
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);

  // Confirm dialog state
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState<'disable' | 'enable'>('disable');
  const [targetUser, setTargetUser] = useState<AdminUser | null>(null);
  const [reason, setReason] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const pageSize = 10;
  const totalPages = Math.ceil(totalCount / pageSize);

  const searchUsers = useCallback(async (searchPage = 1) => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: searchPage.toString(),
        pageSize: pageSize.toString(),
      });

      if (filters.name) params.set('name', filters.name);
      if (filters.email) params.set('email', filters.email);
      if (filters.role !== 'all') params.set('role', filters.role);
      if (filters.chapter !== 'all') params.set('chapter', filters.chapter);
      if (filters.status !== 'all') params.set('status', filters.status);

      const response = await fetch(`/v1/admin/users?${params.toString()}`, {
        headers: { 'Content-Type': 'application/json' },
      });

      if (response.ok) {
        const data = (await response.json()) as PaginatedResponse;
        setUsers(data.users);
        setTotalCount(data.totalCount);
        setPage(data.page);
      }
    } catch {
      // Error handling — show empty results
    } finally {
      setLoading(false);
    }
  }, [filters]);

  function handleSearch() {
    setPage(1);
    void searchUsers(1);
  }

  function handlePageChange(newPage: number) {
    setPage(newPage);
    void searchUsers(newPage);
  }

  function openConfirmDialog(user: AdminUser, action: 'disable' | 'enable') {
    setTargetUser(user);
    setConfirmAction(action);
    setReason('');
    setConfirmOpen(true);
  }

  async function handleConfirmAction() {
    if (!targetUser || !reason.trim()) return;

    try {
      setActionLoading(true);
      const endpoint = confirmAction === 'disable'
        ? `/v1/admin/users/${targetUser.id}/disable`
        : `/v1/admin/users/${targetUser.id}/enable`;

      const response = await fetch(endpoint, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason: reason.trim() }),
      });

      if (response.ok) {
        setConfirmOpen(false);
        void searchUsers(page);
      }
    } catch {
      // Error handling
    } finally {
      setActionLoading(false);
    }
  }

  const statusBadgeClasses: Record<string, string> = {
    active: 'bg-success/20 text-success',
    disabled: 'bg-error/20 text-error',
    locked: 'bg-warning/20 text-warning',
  };

  return (
    <div className="glass-card rounded-xl p-6 space-y-4" data-testid="user-search-panel">
      <h2
        className="text-xl font-semibold"
        style={{ color: 'var(--color-text-primary)' }}
      >
        User Management
      </h2>

      {/* Filters */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
        <div className="flex flex-col gap-1">
          <label
            htmlFor="filter-name"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Name
          </label>
          <input
            id="filter-name"
            type="text"
            placeholder="Search by name..."
            value={filters.name}
            onChange={(e) => setFilters((f) => ({ ...f, name: e.target.value }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary placeholder:text-text-muted outline-none focus-visible:ring-2 focus-visible:ring-primary"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label
            htmlFor="filter-email"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Email
          </label>
          <input
            id="filter-email"
            type="text"
            placeholder="Search by email..."
            value={filters.email}
            onChange={(e) => setFilters((f) => ({ ...f, email: e.target.value }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary placeholder:text-text-muted outline-none focus-visible:ring-2 focus-visible:ring-primary"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label
            htmlFor="filter-role"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Role
          </label>
          <select
            id="filter-role"
            value={filters.role}
            onChange={(e) => setFilters((f) => ({ ...f, role: e.target.value as UserRole }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            <option value="all">All Roles</option>
            <option value="mentor">Mentor</option>
            <option value="mentee">Mentee</option>
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label
            htmlFor="filter-chapter"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Chapter
          </label>
          <select
            id="filter-chapter"
            value={filters.chapter}
            onChange={(e) => setFilters((f) => ({ ...f, chapter: e.target.value as AustralianChapter }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            {CHAPTERS.map((ch) => (
              <option key={ch} value={ch}>
                {ch === 'all' ? 'All Chapters' : ch === 'GoldCoast' ? 'Gold Coast' : ch}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label
            htmlFor="filter-status"
            className="text-xs font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Status
          </label>
          <select
            id="filter-status"
            value={filters.status}
            onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value as UserStatus }))}
            className="px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            <option value="all">All Statuses</option>
            <option value="active">Active</option>
            <option value="disabled">Disabled</option>
            <option value="locked">Locked</option>
          </select>
        </div>

        <div className="flex items-end">
          <button
            onClick={handleSearch}
            className="w-full px-4 py-2 rounded-md text-sm font-medium text-[#0a1628] transition-all"
            style={{ background: 'var(--color-primary)' }}
            aria-label="Search users"
          >
            Search
          </button>
        </div>
      </div>

      {/* Results Table */}
      <div className="overflow-x-auto">
        <table
          className="w-full text-sm"
          role="table"
          aria-label="User search results"
        >
          <thead>
            <tr
              className="border-b border-[rgba(255,255,255,0.08)]"
              style={{ color: 'var(--color-text-secondary)' }}
            >
              <th className="text-left py-3 px-2 font-medium">Name</th>
              <th className="text-left py-3 px-2 font-medium">Email</th>
              <th className="text-left py-3 px-2 font-medium">Role</th>
              <th className="text-left py-3 px-2 font-medium">Chapter</th>
              <th className="text-left py-3 px-2 font-medium">Status</th>
              <th className="text-left py-3 px-2 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="text-center py-8 text-text-muted">
                  Loading...
                </td>
              </tr>
            ) : users.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-8 text-text-muted">
                  No users found. Try adjusting your filters.
                </td>
              </tr>
            ) : (
              users.map((user) => (
                <tr
                  key={user.id}
                  className="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)]"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  <td className="py-3 px-2">{user.displayName}</td>
                  <td className="py-3 px-2 text-text-secondary">{user.email}</td>
                  <td className="py-3 px-2 capitalize">{user.activeRole ?? '—'}</td>
                  <td className="py-3 px-2">{user.awsChapter}</td>
                  <td className="py-3 px-2">
                    <span
                      className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${statusBadgeClasses[user.status] ?? ''}`}
                    >
                      {user.status}
                    </span>
                  </td>
                  <td className="py-3 px-2">
                    {user.status === 'active' || user.status === 'locked' ? (
                      <button
                        onClick={() => openConfirmDialog(user, 'disable')}
                        className="text-xs px-2 py-1 rounded bg-error/20 text-error hover:bg-error/30 transition-colors"
                        aria-label={`Disable user ${user.displayName}`}
                      >
                        Disable
                      </button>
                    ) : (
                      <button
                        onClick={() => openConfirmDialog(user, 'enable')}
                        className="text-xs px-2 py-1 rounded bg-success/20 text-success hover:bg-success/30 transition-colors"
                        aria-label={`Enable user ${user.displayName}`}
                      >
                        Enable
                      </button>
                    )}
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
          aria-label="User search pagination"
        >
          <p className="text-xs text-text-muted">
            Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of{' '}
            {totalCount} users
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

      {/* Confirm Dialog for Disable/Enable */}
      <ConfirmDialog
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        onConfirm={() => void handleConfirmAction()}
        title={confirmAction === 'disable' ? 'Disable User Account' : 'Enable User Account'}
        description={
          <div className="space-y-3">
            <p>
              {confirmAction === 'disable'
                ? `Are you sure you want to disable the account for "${targetUser?.displayName}"? They will be unable to access the platform.`
                : `Are you sure you want to re-enable the account for "${targetUser?.displayName}"?`}
            </p>
            <div className="flex flex-col gap-1">
              <label
                htmlFor="action-reason"
                className="text-xs font-medium text-text-secondary"
              >
                Reason (required)
              </label>
              <textarea
                id="action-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="Enter reason for this action..."
                rows={3}
                className="w-full px-3 py-2 rounded-md text-sm bg-surface border border-[rgba(255,255,255,0.08)] text-text-primary placeholder:text-text-muted outline-none focus-visible:ring-2 focus-visible:ring-primary resize-none"
                required
                aria-required="true"
              />
            </div>
          </div>
        }
        confirmLabel={confirmAction === 'disable' ? 'Disable Account' : 'Enable Account'}
        loading={actionLoading}
        variant={confirmAction === 'disable' ? 'danger' : 'warning'}
      />
    </div>
  );
}

export default UserSearchPanel;
