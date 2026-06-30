import { describe, it, expect } from 'vitest';
import { UserId } from './user-id';
import { MentorId } from './mentor-id';
import { MenteeId } from './mentee-id';
import { SessionId } from './session-id';
import { Email } from './email';
import { OpportunityPostingId } from './opportunity-posting-id';
import { MeetupEventId } from './meetup-event-id';
import { NotificationId } from './notification-id';
import { LockId } from './lock-id';
import { AdminUserId } from './admin-user-id';

const VALID_GUID = '550e8400-e29b-41d4-a716-446655440000';
const INVALID_GUID = 'not-a-guid';

describe('UserId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = UserId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
    expect(result.value!.value).toBe(VALID_GUID);
  });

  it('fails with an invalid GUID', () => {
    const result = UserId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
    expect(result.error).toBe('UserId must be a valid GUID');
  });

  it('fails with empty string', () => {
    const result = UserId.create('');
    expect(result.isFailure).toBe(true);
  });

  it('fromString returns the value object on valid input', () => {
    const id = UserId.fromString(VALID_GUID);
    expect(id.value).toBe(VALID_GUID);
  });

  it('fromString throws on invalid input', () => {
    expect(() => UserId.fromString(INVALID_GUID)).toThrow();
  });

  it('equals another UserId with the same value', () => {
    const a = UserId.fromString(VALID_GUID);
    const b = UserId.fromString(VALID_GUID);
    expect(a.equals(b)).toBe(true);
  });

  it('is not equal to null', () => {
    const id = UserId.fromString(VALID_GUID);
    expect(id.equals(null)).toBe(false);
  });

  it('toString returns the GUID string', () => {
    const id = UserId.fromString(VALID_GUID);
    expect(id.toString()).toBe(VALID_GUID);
  });
});

describe('MentorId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = MentorId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = MentorId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('MenteeId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = MenteeId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = MenteeId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('SessionId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = SessionId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = SessionId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('Email', () => {
  it('creates successfully with a valid email', () => {
    const result = Email.create('user@example.com');
    expect(result.isSuccess).toBe(true);
    expect(result.value!.value).toBe('user@example.com');
  });

  it('normalizes email to lowercase', () => {
    const result = Email.create('User@Example.COM');
    expect(result.isSuccess).toBe(true);
    expect(result.value!.value).toBe('user@example.com');
  });

  it('fails with missing @ sign', () => {
    const result = Email.create('invalid-email');
    expect(result.isFailure).toBe(true);
    expect(result.error).toBe('Email format is invalid');
  });

  it('fails with empty string', () => {
    const result = Email.create('');
    expect(result.isFailure).toBe(true);
  });

  it('fails with missing domain', () => {
    const result = Email.create('user@');
    expect(result.isFailure).toBe(true);
  });

  it('fails with missing TLD', () => {
    const result = Email.create('user@domain');
    expect(result.isFailure).toBe(true);
  });

  it('fails with single char TLD', () => {
    const result = Email.create('user@domain.a');
    expect(result.isFailure).toBe(true);
  });

  it('accepts valid formats with subdomains', () => {
    const result = Email.create('user@sub.domain.com.au');
    expect(result.isSuccess).toBe(true);
  });

  it('accepts plus addressing', () => {
    const result = Email.create('user+tag@example.com');
    expect(result.isSuccess).toBe(true);
  });

  it('rejects email exceeding 254 characters', () => {
    const longLocal = 'a'.repeat(243); // 243 + '@example.com' = 255 chars
    const result = Email.create(`${longLocal}@example.com`);
    expect(result.isFailure).toBe(true);
    expect(result.error).toBe('Email must not exceed 254 characters');
  });

  it('equals another Email with the same normalized value', () => {
    const a = Email.fromString('Test@Example.com');
    const b = Email.fromString('test@example.com');
    expect(a.equals(b)).toBe(true);
  });
});

describe('OpportunityPostingId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = OpportunityPostingId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = OpportunityPostingId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('MeetupEventId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = MeetupEventId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = MeetupEventId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('NotificationId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = NotificationId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = NotificationId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('LockId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = LockId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = LockId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});

describe('AdminUserId', () => {
  it('creates successfully with a valid GUID', () => {
    const result = AdminUserId.create(VALID_GUID);
    expect(result.isSuccess).toBe(true);
  });

  it('fails with an invalid GUID', () => {
    const result = AdminUserId.create(INVALID_GUID);
    expect(result.isFailure).toBe(true);
  });
});
