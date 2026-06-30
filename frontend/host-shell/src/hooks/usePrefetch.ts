import { useCallback, useRef } from 'react';

/**
 * usePrefetch — preloads data when user hovers over a link.
 * Uses a simple fetch cache to avoid duplicate requests.
 */
const prefetchCache = new Map<string, Promise<unknown>>();

export function usePrefetch() {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const prefetch = useCallback((url: string) => {
    // Only prefetch if not already in cache
    if (prefetchCache.has(url)) return;

    // Delay 150ms to avoid prefetching on accidental hover
    timerRef.current = setTimeout(() => {
      const promise = fetch(url).then(r => r.json()).catch(() => null);
      prefetchCache.set(url, promise);
    }, 150);
  }, []);

  const cancelPrefetch = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  return { prefetch, cancelPrefetch };
}

// Get prefetched data (returns null if not prefetched or still loading)
export function getPrefetchedData<T>(url: string): T | null {
  const cached = prefetchCache.get(url);
  if (!cached) return null;
  let result: T | null = null;
  cached.then((data) => { result = data as T; }).catch(() => {});
  return result;
}
