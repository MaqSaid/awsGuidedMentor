/**
 * NotificationPanel — Last 50 notifications, unread indicator,
 * click-to-navigate, and batch mark-all-read.
 *
 * Requirements: 12.3, 12.4, 12.5, 12.7
 */
import { useNotifications, useMarkNotificationRead, useMarkAllRead, useUnreadCount } from '../hooks/useApi';
import type { Notification, NotificationType } from '../types';

const NOTIFICATION_ICONS: Record<NotificationType, string> = {
  request_sent: '📤',
  request_accepted: '✅',
  request_declined: '❌',
  session_plan_ready: '📋',
  completion_marked: '🎉',
  reminder: '🔔',
};

export function NotificationPanel() {
  const { data: notifications, isLoading, isError, refetch } = useNotifications();
  const { data: unreadData } = useUnreadCount();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllRead();

  const unreadCount = unreadData?.count ?? 0;

  if (isLoading) {
    return (
      <div data-testid="engagement-notification-panel" className="p-6 space-y-4">
        <h2 className="text-xl font-bold text-text-primary">Notifications</h2>
        <div className="space-y-3">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="glass-card p-4 rounded-lg animate-pulse h-16 bg-[rgba(255,255,255,0.06)]" role="status" aria-label="Loading" />
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div data-testid="engagement-notification-panel" className="p-6 space-y-4">
        <h2 className="text-xl font-bold text-text-primary">Notifications</h2>
        <div className="glass-card p-6 text-center" role="alert">
          <p className="text-text-secondary mb-4">Failed to load notifications.</p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-primary text-[#0a1628] font-semibold rounded-md hover:opacity-90 transition-opacity"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  const items = notifications ?? [];

  return (
    <div data-testid="engagement-notification-panel" className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h2 className="text-xl font-bold text-text-primary">Notifications</h2>
          {unreadCount > 0 && (
            <span
              className="inline-flex items-center justify-center h-5 min-w-5 px-1.5 rounded-full bg-primary text-[#0a1628] text-xs font-bold"
              aria-label={`${unreadCount} unread notifications`}
            >
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </div>
        {unreadCount > 0 && (
          <button
            onClick={() => markAllRead.mutate()}
            disabled={markAllRead.isPending}
            className="text-sm text-primary hover:text-primary/80 transition-colors disabled:opacity-50"
            aria-label="Mark all notifications as read"
          >
            {markAllRead.isPending ? 'Marking...' : 'Mark all read'}
          </button>
        )}
      </div>

      {items.length === 0 ? (
        <div className="glass-card p-8 rounded-lg text-center">
          <p className="text-text-muted">No notifications yet. You'll see updates here as your mentorship journey progresses.</p>
        </div>
      ) : (
        <ul className="space-y-2" role="list" aria-label="Notification list">
          {items.map((notification) => (
            <NotificationItem
              key={notification.notificationId}
              notification={notification}
              onRead={() => markRead.mutate(notification.notificationId)}
            />
          ))}
        </ul>
      )}
    </div>
  );
}

function NotificationItem({
  notification,
  onRead,
}: {
  notification: Notification;
  onRead: () => void;
}) {
  const handleClick = () => {
    if (!notification.isRead) {
      onRead();
    }
    // Navigate via actionUrl
    if (notification.actionUrl) {
      window.location.href = notification.actionUrl;
    }
  };

  const timeAgo = getTimeAgo(notification.createdAt);

  return (
    <li>
      <button
        onClick={handleClick}
        className={`
          w-full text-left glass-card p-4 rounded-lg flex items-start gap-3 
          hover:ring-1 hover:ring-primary/20 transition-all cursor-pointer
          ${!notification.isRead ? 'border-l-2 border-l-primary' : ''}
        `}
        aria-label={`${notification.isRead ? '' : 'Unread: '}${notification.message}`}
      >
        <span className="text-lg flex-shrink-0" aria-hidden="true">
          {NOTIFICATION_ICONS[notification.type] ?? '📩'}
        </span>
        <div className="flex-1 min-w-0">
          <p className={`text-sm ${notification.isRead ? 'text-text-secondary' : 'text-text-primary font-medium'}`}>
            {notification.message}
          </p>
          <p className="text-xs text-text-muted mt-0.5">{timeAgo}</p>
        </div>
        {!notification.isRead && (
          <span className="h-2 w-2 rounded-full bg-primary flex-shrink-0 mt-1.5" aria-hidden="true" />
        )}
      </button>
    </li>
  );
}

function getTimeAgo(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMinutes = Math.floor(diffMs / 60_000);

  if (diffMinutes < 1) return 'Just now';
  if (diffMinutes < 60) return `${diffMinutes}m ago`;
  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString('en-AU', { month: 'short', day: 'numeric' });
}

export default NotificationPanel;
