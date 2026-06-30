import { Result } from '../base/result';

/**
 * Strongly-typed NotificationId value object.
 * Wraps a GUID string to distinguish notification identifiers from other IDs.
 */
export class NotificationId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<NotificationId> {
    if (!value || !NotificationId.GUID_REGEX.test(value)) {
      return Result.failure('NotificationId must be a valid GUID');
    }
    return Result.success(new NotificationId(value));
  }

  static fromString(value: string): NotificationId {
    const result = NotificationId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: NotificationId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
