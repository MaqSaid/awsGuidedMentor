import { Result } from '../base/result';

/**
 * Strongly-typed UserId value object.
 * Wraps a GUID string to prevent primitive obsession and misuse of IDs.
 */
export class UserId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<UserId> {
    if (!value || !UserId.GUID_REGEX.test(value)) {
      return Result.failure('UserId must be a valid GUID');
    }
    return Result.success(new UserId(value));
  }

  static fromString(value: string): UserId {
    const result = UserId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: UserId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
