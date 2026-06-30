import { Result } from '../base/result';

/**
 * Strongly-typed MeetupEventId value object.
 * Wraps a GUID string to distinguish meetup event identifiers from other IDs.
 */
export class MeetupEventId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<MeetupEventId> {
    if (!value || !MeetupEventId.GUID_REGEX.test(value)) {
      return Result.failure('MeetupEventId must be a valid GUID');
    }
    return Result.success(new MeetupEventId(value));
  }

  static fromString(value: string): MeetupEventId {
    const result = MeetupEventId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: MeetupEventId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
