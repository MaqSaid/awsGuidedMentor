import { describe, it, expect } from 'vitest';
import { Result } from './result';

describe('Result', () => {
  describe('success', () => {
    it('creates a successful result with a value', () => {
      const result = Result.success(42);
      expect(result.isSuccess).toBe(true);
      expect(result.isFailure).toBe(false);
      expect(result.value).toBe(42);
      expect(result.error).toBeUndefined();
    });

    it('creates a successful void result with ok()', () => {
      const result = Result.ok();
      expect(result.isSuccess).toBe(true);
      expect(result.isFailure).toBe(false);
      expect(result.error).toBeUndefined();
    });
  });

  describe('failure', () => {
    it('creates a failed result with an error message', () => {
      const result = Result.failure<number>('something went wrong');
      expect(result.isSuccess).toBe(false);
      expect(result.isFailure).toBe(true);
      expect(result.value).toBeUndefined();
      expect(result.error).toBe('something went wrong');
    });
  });

  describe('getValueOrThrow', () => {
    it('returns the value on success', () => {
      const result = Result.success('hello');
      expect(result.getValueOrThrow()).toBe('hello');
    });

    it('throws on failure', () => {
      const result = Result.failure<string>('err');
      expect(() => result.getValueOrThrow()).toThrow('Cannot access value of a failed result: err');
    });
  });

  describe('map', () => {
    it('transforms the value on success', () => {
      const result = Result.success(5).map((v) => v * 2);
      expect(result.isSuccess).toBe(true);
      expect(result.value).toBe(10);
    });

    it('propagates error on failure', () => {
      const result = Result.failure<number>('oops').map((v) => v * 2);
      expect(result.isFailure).toBe(true);
      expect(result.error).toBe('oops');
    });
  });
});
