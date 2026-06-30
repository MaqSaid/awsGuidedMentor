import { Result } from '../base/result';

/**
 * Email value object with basic format validation.
 * Validates that the email has a standard user@domain.tld format.
 */
export class Email {
  /**
   * Basic email regex: requires characters before @, domain segments, and a TLD of 2+ chars.
   * This is intentionally not RFC 5322 full-spec — it covers practical use cases.
   */
  private static readonly EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

  private constructor(public readonly value: string) {}

  static create(value: string): Result<Email> {
    if (!value) {
      return Result.failure('Email format is invalid');
    }

    if (value.length > 254) {
      return Result.failure('Email must not exceed 254 characters');
    }

    if (!Email.EMAIL_REGEX.test(value)) {
      return Result.failure('Email format is invalid');
    }

    return Result.success(new Email(value.toLowerCase().trim()));
  }

  static fromString(value: string): Email {
    const result = Email.create(value);
    return result.getValueOrThrow();
  }

  equals(other: Email | null | undefined): boolean {
    if (!other) return false;
    return this.value === other.value;
  }

  toString(): string {
    return this.value;
  }
}
