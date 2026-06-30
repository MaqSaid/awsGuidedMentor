import { Result } from '../base/result';

/**
 * Strongly-typed OpportunityPostingId value object.
 * Wraps a GUID string to distinguish opportunity posting identifiers from other IDs.
 */
export class OpportunityPostingId {
  private static readonly GUID_REGEX =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<OpportunityPostingId> {
    if (!value || !OpportunityPostingId.GUID_REGEX.test(value)) {
      return Result.failure('OpportunityPostingId must be a valid GUID');
    }
    return Result.success(new OpportunityPostingId(value));
  }

  static fromString(value: string): OpportunityPostingId {
    const result = OpportunityPostingId.create(value);
    return result.getValueOrThrow();
  }

  equals(other: OpportunityPostingId | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
