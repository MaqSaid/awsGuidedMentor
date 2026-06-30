import { Result } from '../base/result';

/**
 * Strongly-typed MentorId value object.
 * Wraps a GUID string to distinguish mentor identifiers from other IDs.
 */
export class MentorId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<MentorId> {
    if (!value || !MentorId.GUID_REGEX.test(value)) {
      return Result.failure('MentorId must be a valid GUID');
    }
    return Result.success(new MentorId(value));
  }

  static fromString(value: string): MentorId {
    const result = MentorId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: MentorId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
