import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useRetry } from './useRetry';

describe('useRetry', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('starts in initial state', () => {
    const { result } = renderHook(() => useRetry());

    expect(result.current.isRetrying).toBe(false);
    expect(result.current.attempt).toBe(0);
    expect(result.current.exhausted).toBe(false);
  });

  it('sets isRetrying to true during operation', async () => {
    const { result } = renderHook(() => useRetry());

    let resolvePromise: () => void;
    const operation = () =>
      new Promise<void>((resolve) => {
        resolvePromise = resolve;
      });

    act(() => {
      result.current.retry(operation);
    });

    expect(result.current.isRetrying).toBe(true);
    expect(result.current.attempt).toBe(1);

    await act(async () => {
      resolvePromise!();
    });

    expect(result.current.isRetrying).toBe(false);
    expect(result.current.attempt).toBe(0);
  });

  it('calls onSuccess when operation succeeds', async () => {
    const onSuccess = vi.fn();
    const { result } = renderHook(() => useRetry({ onSuccess }));

    await act(async () => {
      await result.current.retry(async () => {});
    });

    expect(onSuccess).toHaveBeenCalledTimes(1);
    expect(result.current.exhausted).toBe(false);
  });

  it('retries failed operations up to maxRetries', async () => {
    const onExhausted = vi.fn();
    const operation = vi.fn().mockRejectedValue(new Error('fail'));

    const { result } = renderHook(() =>
      useRetry({ maxRetries: 3, baseDelayMs: 100, onExhausted })
    );

    await act(async () => {
      const retryPromise = result.current.retry(operation);
      // Advance through all backoff timers
      await vi.advanceTimersByTimeAsync(100); // 1st backoff
      await vi.advanceTimersByTimeAsync(200); // 2nd backoff
      await retryPromise;
    });

    expect(operation).toHaveBeenCalledTimes(3);
    expect(result.current.exhausted).toBe(true);
    expect(onExhausted).toHaveBeenCalledTimes(1);
  });

  it('succeeds on second attempt after first failure', async () => {
    const onSuccess = vi.fn();
    let callCount = 0;
    const operation = vi.fn().mockImplementation(async () => {
      callCount++;
      if (callCount === 1) throw new Error('first attempt fails');
    });

    const { result } = renderHook(() =>
      useRetry({ maxRetries: 3, baseDelayMs: 100, onSuccess })
    );

    await act(async () => {
      const retryPromise = result.current.retry(operation);
      await vi.advanceTimersByTimeAsync(100); // backoff after first failure
      await retryPromise;
    });

    expect(operation).toHaveBeenCalledTimes(2);
    expect(onSuccess).toHaveBeenCalledTimes(1);
    expect(result.current.exhausted).toBe(false);
    expect(result.current.isRetrying).toBe(false);
  });

  it('reset clears all state', async () => {
    const operation = vi.fn().mockRejectedValue(new Error('fail'));

    const { result } = renderHook(() =>
      useRetry({ maxRetries: 2, baseDelayMs: 100 })
    );

    await act(async () => {
      const retryPromise = result.current.retry(operation);
      await vi.advanceTimersByTimeAsync(100);
      await retryPromise;
    });

    expect(result.current.exhausted).toBe(true);

    act(() => {
      result.current.reset();
    });

    expect(result.current.isRetrying).toBe(false);
    expect(result.current.attempt).toBe(0);
    expect(result.current.exhausted).toBe(false);
  });
});
