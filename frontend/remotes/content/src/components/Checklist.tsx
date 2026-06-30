/**
 * Checklist — interactive checklist with optimistic UI updates.
 * Supports immediate toggle, reverts on failure, and retry button.
 *
 * Requirements: 8.1, 8.3, 8.4
 */
import { useState, useCallback } from 'react';
import type { ChecklistUpdate } from '../types';

export interface ChecklistProps {
  title: string;
  items: string[];
  checkedState: boolean[];
  type: 'prework' | 'followup';
  onToggle: (update: ChecklistUpdate) => Promise<void>;
  className?: string;
}

interface ItemError {
  index: number;
  message: string;
}

export function Checklist({
  title,
  items,
  checkedState,
  type,
  onToggle,
  className = '',
}: ChecklistProps) {
  const [optimisticState, setOptimisticState] = useState<boolean[]>(checkedState);
  const [pendingItems, setPendingItems] = useState<Set<number>>(new Set());
  const [errors, setErrors] = useState<ItemError[]>([]);

  // Sync optimistic state when server state changes (e.g., after refetch)
  if (
    checkedState.length !== optimisticState.length ||
    checkedState.some((v, i) => !pendingItems.has(i) && v !== optimisticState[i])
  ) {
    const merged = checkedState.map((serverVal, i) =>
      pendingItems.has(i) ? optimisticState[i]! : serverVal
    );
    setOptimisticState(merged);
  }

  const handleToggle = useCallback(
    async (index: number) => {
      const previousValue = optimisticState[index]!;
      const newValue = !previousValue;

      // Optimistic update — immediate visual feedback
      setOptimisticState((prev) => {
        const next = [...prev];
        next[index] = newValue;
        return next;
      });
      setPendingItems((prev) => new Set(prev).add(index));
      setErrors((prev) => prev.filter((e) => e.index !== index));

      try {
        await onToggle({ type, index, checked: newValue });
      } catch {
        // Revert on failure
        setOptimisticState((prev) => {
          const reverted = [...prev];
          reverted[index] = previousValue;
          return reverted;
        });
        setErrors((prev) => [
          ...prev.filter((e) => e.index !== index),
          { index, message: 'Failed to save. Please try again.' },
        ]);
      } finally {
        setPendingItems((prev) => {
          const next = new Set(prev);
          next.delete(index);
          return next;
        });
      }
    },
    [optimisticState, onToggle, type]
  );

  const handleRetry = useCallback(
    (index: number) => {
      handleToggle(index);
    },
    [handleToggle]
  );

  return (
    <section aria-label={title} className={className}>
      <h3 className="text-lg font-semibold text-[var(--color-text-primary)] mb-3">
        {title}
      </h3>
      <ul className="flex flex-col gap-2" role="list">
        {items.map((item, index) => {
          const isChecked = optimisticState[index] ?? false;
          const isPending = pendingItems.has(index);
          const error = errors.find((e) => e.index === index);

          return (
            <li key={index} className="flex flex-col gap-1">
              <label
                className={[
                  'flex items-center gap-3 p-3 rounded-[var(--radius-md)] cursor-pointer',
                  'transition-colors hover:bg-[rgba(255,255,255,0.03)]',
                  isPending ? 'opacity-70' : '',
                  isChecked ? 'line-through text-[var(--color-text-muted)]' : '',
                ].join(' ')}
              >
                <input
                  type="checkbox"
                  checked={isChecked}
                  onChange={() => handleToggle(index)}
                  disabled={isPending}
                  aria-label={`${isChecked ? 'Uncheck' : 'Check'}: ${item}`}
                  className="h-5 w-5 rounded border-2 border-[rgba(255,255,255,0.2)] bg-transparent checked:bg-[var(--color-primary)] checked:border-[var(--color-primary)] focus-visible:ring-2 focus-visible:ring-[var(--color-primary)] accent-[var(--color-primary)] cursor-pointer"
                />
                <span
                  className={[
                    'text-sm',
                    isChecked
                      ? 'text-[var(--color-text-muted)]'
                      : 'text-[var(--color-text-primary)]',
                  ].join(' ')}
                >
                  {item}
                </span>
              </label>
              {error && (
                <div
                  className="flex items-center gap-2 ml-8 text-xs text-[var(--color-error)]"
                  role="alert"
                >
                  <span>{error.message}</span>
                  <button
                    type="button"
                    onClick={() => handleRetry(index)}
                    className="underline hover:text-[var(--color-text-primary)] focus-visible:ring-2 focus-visible:ring-[var(--color-primary)] rounded px-1"
                    aria-label={`Retry saving: ${item}`}
                  >
                    Retry
                  </button>
                </div>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
