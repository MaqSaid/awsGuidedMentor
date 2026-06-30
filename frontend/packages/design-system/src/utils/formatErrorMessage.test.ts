import { describe, it, expect } from 'vitest';
import { formatErrorMessage } from './formatErrorMessage';

describe('formatErrorMessage', () => {
  it('returns friendly message for 400 status code', () => {
    const result = formatErrorMessage({ statusCode: 400 });
    expect(result.message).toContain('incorrect');
    expect(result.canRetry).toBe(false);
  });

  it('returns friendly message for 401 status code', () => {
    const result = formatErrorMessage({ statusCode: 401 });
    expect(result.message).toContain('session has expired');
    expect(result.canRetry).toBe(false);
  });

  it('returns friendly message for 403 status code', () => {
    const result = formatErrorMessage({ statusCode: 403 });
    expect(result.message).toContain('permission');
    expect(result.canRetry).toBe(false);
  });

  it('returns friendly message for 404 status code', () => {
    const result = formatErrorMessage({ statusCode: 404 });
    expect(result.message).toContain("couldn't find");
    expect(result.canRetry).toBe(false);
  });

  it('returns retryable message for 500 status code', () => {
    const result = formatErrorMessage({ statusCode: 500 });
    expect(result.message).toContain('Something went wrong');
    expect(result.canRetry).toBe(true);
  });

  it('returns retryable message for 503 status code', () => {
    const result = formatErrorMessage({ statusCode: 503 });
    expect(result.message).toContain('temporarily unavailable');
    expect(result.canRetry).toBe(true);
  });

  it('returns retryable message for 429 status code', () => {
    const result = formatErrorMessage({ statusCode: 429 });
    expect(result.message).toContain('too many requests');
    expect(result.canRetry).toBe(true);
  });

  it('prioritizes error code over status code', () => {
    const result = formatErrorMessage({
      statusCode: 500,
      errorCode: 'NETWORK_ERROR',
    });
    expect(result.message).toContain('internet connection');
    expect(result.canRetry).toBe(true);
  });

  it('handles MENTOR_AT_CAPACITY error code', () => {
    const result = formatErrorMessage({ errorCode: 'MENTOR_AT_CAPACITY' });
    expect(result.message).toContain('maximum number of mentees');
    expect(result.canRetry).toBe(false);
  });

  it('handles AI_UNAVAILABLE error code', () => {
    const result = formatErrorMessage({ errorCode: 'AI_UNAVAILABLE' });
    expect(result.message).toContain('AI features');
    expect(result.canRetry).toBe(true);
  });

  it('handles FILE_TOO_LARGE error code', () => {
    const result = formatErrorMessage({ errorCode: 'FILE_TOO_LARGE' });
    expect(result.message).toContain('too large');
    expect(result.canRetry).toBe(false);
  });

  it('detects network errors from raw message', () => {
    const result = formatErrorMessage({ rawMessage: 'Failed to fetch data' });
    expect(result.message).toContain('internet connection');
    expect(result.canRetry).toBe(true);
  });

  it('detects timeout errors from raw message', () => {
    const result = formatErrorMessage({ rawMessage: 'Request timed out' });
    expect(result.message).toContain('took too long');
    expect(result.canRetry).toBe(true);
  });

  it('returns generic fallback for unknown errors', () => {
    const result = formatErrorMessage({});
    expect(result.message).toBe('Something went wrong. Please try again.');
    expect(result.canRetry).toBe(true);
  });

  it('returns generic fallback for unknown status codes', () => {
    const result = formatErrorMessage({ statusCode: 999 });
    expect(result.message).toBe('Something went wrong. Please try again.');
    expect(result.canRetry).toBe(true);
  });
});
