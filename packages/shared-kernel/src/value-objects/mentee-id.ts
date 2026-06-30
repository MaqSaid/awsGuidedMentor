import { Result } from '../base/result';

/**
 * Strongly-typed MenteeId value object.
 * Wraps a GUID string to distinguish mentee identifiers from other IDs.
 */
export class MenteeId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<MenteeId> {
    if (!value || !MenteeId.GUID_REGEX.test(value)) {
      return Result.failure('MenteeId must be a valid GUID');
    }
    return Result.success(new MenteeId(value));
  }

  static fromString(value: string): MenteeId {
    const result = MenteeId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: MenteeId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
