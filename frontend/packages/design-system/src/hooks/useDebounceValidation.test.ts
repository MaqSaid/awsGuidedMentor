import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useDebounceValidation } from './useDebounceValidation';

describe('useDebounceValidation', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('starts in idle state', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: (v) => (v.length < 3 ? 'Too short' : null) }],
      })
    );

    expect(result.current.state).toBe('idle');
    expect(result.current.error).toBeNull();
    expect(result.current.touched).toBe(false);
  });

  it('transitions to validating state immediately on validate call', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: (v) => (v.length < 3 ? 'Too short' : null) }],
      })
    );

    act(() => {
      result.current.validate('ab');
    });

    expect(result.current.state).toBe('validating');
    expect(result.current.touched).toBe(true);
  });

  it('returns invalid state with error after 300ms debounce for failing input', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: (v) => (v.length < 3 ? 'Too short' : null) }],
      })
    );

    act(() => {
      result.current.validate('ab');
    });

    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(result.current.state).toBe('invalid');
    expect(result.current.error).toBe('Too short');
  });

  it('returns valid state after 300ms debounce for passing input', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: (v) => (v.length < 3 ? 'Too short' : null) }],
      })
    );

    act(() => {
      result.current.validate('abc');
    });

    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(result.current.state).toBe('valid');
    expect(result.current.error).toBeNull();
  });

  it('debounces multiple rapid validate calls (only last fires)', () => {
    const validateFn = vi.fn((v: string) => (v.length < 3 ? 'Too short' : null));
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: validateFn }],
      })
    );

    act(() => {
      result.current.validate('a');
      result.current.validate('ab');
      result.current.validate('abc');
    });

    act(() => {
      vi.advanceTimersByTime(300);
    });

    // Only the last validation should have run after debounce
    expect(validateFn).toHaveBeenCalledTimes(1);
    expect(validateFn).toHaveBeenCalledWith('abc');
    expect(result.current.state).toBe('valid');
  });

  it('applies custom debounce delay', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: (v) => (v.length < 3 ? 'Too short' : null) }],
        debounceMs: 500,
      })
    );

    act(() => {
      result.current.validate('ab');
    });

    // After 300ms, still validating
    act(() => {
      vi.advanceTimersByTime(300);
    });
    expect(result.current.state).toBe('validating');

    // After 500ms total, validation completes
    act(() => {
      vi.advanceTimersByTime(200);
    });
    expect(result.current.state).toBe('invalid');
  });

  it('returns first failing rule error when multiple rules exist', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [
          { validate: (v) => (v.length < 3 ? 'Too short' : null) },
          { validate: (v) => (!/[A-Z]/.test(v) ? 'Must contain uppercase' : null) },
        ],
      })
    );

    act(() => {
      result.current.validate('ab');
    });
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(result.current.error).toBe('Too short');
  });

  it('returns second rule error when first passes', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [
          { validate: (v) => (v.length < 3 ? 'Too short' : null) },
          { validate: (v) => (!/[A-Z]/.test(v) ? 'Must contain uppercase' : null) },
        ],
      })
    );

    act(() => {
      result.current.validate('abc');
    });
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(result.current.error).toBe('Must contain uppercase');
  });

  it('resets state to idle', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: (v) => (v.length < 3 ? 'Too short' : null) }],
      })
    );

    act(() => {
      result.current.validate('ab');
    });
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(result.current.state).toBe('invalid');

    act(() => {
      result.current.reset();
    });

    expect(result.current.state).toBe('idle');
    expect(result.current.error).toBeNull();
    expect(result.current.touched).toBe(false);
  });

  it('provides tooltip from rules', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [
          {
            validate: (v) => (v.length < 3 ? 'Too short' : null),
            tooltip: 'Must be at least 3 characters',
          },
        ],
      })
    );

    expect(result.current.tooltip).toBe('Must be at least 3 characters');
  });

  it('touch marks field as touched', () => {
    const { result } = renderHook(() =>
      useDebounceValidation({
        rules: [{ validate: () => null }],
      })
    );

    expect(result.current.touched).toBe(false);

    act(() => {
      result.current.touch();
    });

    expect(result.current.touched).toBe(true);
  });
});
