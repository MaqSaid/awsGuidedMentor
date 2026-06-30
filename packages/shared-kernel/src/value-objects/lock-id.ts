import { Result } from '../base/result';

/**
 * Strongly-typed LockId value object.
 * Wraps a GUID string to distinguish mentor lock identifiers from other IDs.
 */
export class LockId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<LockId> {
    if (!value || !LockId.GUID_REGEX.test(value)) {
      return Result.failure('LockId must be a valid GUID');
    }
    return Result.success(new LockId(value));
  }

  static fromString(value: string): LockId {
    const result = LockId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: LockId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
