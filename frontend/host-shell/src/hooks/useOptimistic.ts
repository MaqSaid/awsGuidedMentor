import { useState, useCallback, useTransition } from 'react';

/**
 * useOptimisticToggle — optimistically toggles a boolean value.
 * Reverts to server state on failure.
 */
export function useOptimisticToggle(
  serverState: boolean,
  updateFn: (newValue: boolean) => Promise<void>
) {
  const [optimistic, setOptimistic] = useState(serverState);
  const [, startTransition] = useTransition();

  const toggle = useCallback(async () => {
    const newValue = !optimistic;
    setOptimistic(newValue); // optimistic update
    try {
      startTransition(() => {});
      await updateFn(newValue);
    } catch {
      setOptimistic(serverState); // revert on failure
    }
  }, [optimistic, serverState, updateFn, startTransition]);

  return [optimistic, toggle] as const;
}

/**
 * useOptimisticList — optimistically marks an item in a list.
 * Useful for read/unread toggling or similar list-level mutations.
 */
export function useOptimisticList<T extends { id: string }>(
  items: T[],
  markFn: (id: string) => Promise<void>
) {
  const [optimisticItems, setOptimisticItems] = useState(items);

  const markItem = useCallback(async (id: string) => {
    setOptimisticItems((current) =>
      current.map(item => item.id === id ? { ...item, isRead: true } : item)
    );
    await markFn(id);
  }, [markFn]);

  return [optimisticItems, markItem] as const;
}
