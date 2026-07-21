/**
 * Route-to-remote entry URL mapping for federated module preloading.
 * Only routes that correspond to federated remotes should be listed here.
 * Host-shell routes (e.g., /dashboard, /settings) are NOT included.
 */
export const REMOTE_ENTRIES: Record<string, string> = {
  '/browse': '/remotes/mentoring/remoteEntry.js',
  '/opportunities': '/remotes/mentoring/remoteEntry.js',
  '/notifications': '/remotes/engagement/remoteEntry.js',
};
