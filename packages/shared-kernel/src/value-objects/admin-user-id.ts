import { Result } from '../base/result';

/**
 * Strongly-typed AdminUserId value object.
 * Wraps a GUID string to distinguish admin user identifiers from other IDs.
 */
export class AdminUserId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<AdminUserId> {
    if (!value || !AdminUserId.GUID_REGEX.test(value)) {
      return Result.failure('AdminUserId must be a valid GUID');
    }
    return Result.success(new AdminUserId(value));
  }

  static fromString(value: string): AdminUserId {
    const result = AdminUserId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: AdminUserId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
