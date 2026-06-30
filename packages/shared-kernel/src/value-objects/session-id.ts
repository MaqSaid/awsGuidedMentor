import { Result } from '../base/result';

/**
 * Strongly-typed SessionId value object.
 * Wraps a GUID string to distinguish session identifiers from other IDs.
 */
export class SessionId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<SessionId> {
    if (!value || !SessionId.GUID_REGEX.test(value)) {
      return Result.failure('SessionId must be a valid GUID');
    }
    return Result.success(new SessionId(value));
  }

  static fromString(value: string): SessionId {
    const result = SessionId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: SessionId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
