import fc from 'fast-check';

const UPPERCASE = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
const LOWERCASE = 'abcdefghijklmnopqrstuvwxyz';
const DIGITS = '0123456789';
const SPECIAL = '!@#$%^&*()_+-=[]{}|;:,.<>?';

/**
 * Generates a valid password that meets all constraints:
 * - At least 8 characters
 * - At least 1 uppercase letter
 * - At least 1 lowercase letter
 * - At least 1 number
 * - At least 1 special character
 */
export const arbValidPassword = (): fc.Arbitrary<string> =>
  fc.tuple(
    // Guarantee at least one of each required character type
    fc.constantFrom(...UPPERCASE.split('')),
    fc.constantFrom(...LOWERCASE.split('')),
    fc.constantFrom(...DIGITS.split('')),
    fc.constantFrom(...SPECIAL.split('')),
    // Fill remaining 4-28 characters (total 8-32)
    fc.stringOf(
      fc.constantFrom(...(UPPERCASE + LOWERCASE + DIGITS + SPECIAL).split('')),
      { minLength: 4, maxLength: 28 }
    ),
  ).map(([upper, lower, digit, special, rest]) => {
    // Combine and shuffle to avoid predictable positions
    const chars = (upper + lower + digit + special + rest).split('');
    // Fisher-Yates shuffle using a deterministic approach for reproducibility
    for (let i = chars.length - 1; i > 0; i--) {
      const j = i % (i + 1); // Simple deterministic "shuffle"
      [chars[i], chars[j]] = [chars[j], chars[i]];
    }
    return chars.join('');
  });

/**
 * Generates an invalid password that fails at least one constraint.
 * The generated password will be missing at least one required character class.
 */
export const arbInvalidPassword = (): fc.Arbitrary<{ password: string; reason: string }> =>
  fc.oneof(
    // Too short (1-7 characters with all types)
    fc.tuple(
      fc.constantFrom(...UPPERCASE.split('')),
      fc.constantFrom(...LOWERCASE.split('')),
      fc.constantFrom(...DIGITS.split('')),
      fc.constantFrom(...SPECIAL.split('')),
      fc.stringOf(
        fc.constantFrom(...(UPPERCASE + LOWERCASE + DIGITS + SPECIAL).split('')),
        { minLength: 0, maxLength: 3 }
      ),
    ).map(([u, l, d, s, rest]) => ({
      password: (u + l + d + s + rest).slice(0, 7),
      reason: 'too_short',
    })),

    // No uppercase
    fc.stringOf(
      fc.constantFrom(...(LOWERCASE + DIGITS + SPECIAL).split('')),
      { minLength: 8, maxLength: 20 }
    ).filter(s =>
      /[a-z]/.test(s) && /[0-9]/.test(s) && /[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(s)
    ).map(password => ({ password, reason: 'no_uppercase' })),

    // No lowercase
    fc.stringOf(
      fc.constantFrom(...(UPPERCASE + DIGITS + SPECIAL).split('')),
      { minLength: 8, maxLength: 20 }
    ).filter(s =>
      /[A-Z]/.test(s) && /[0-9]/.test(s) && /[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(s)
    ).map(password => ({ password, reason: 'no_lowercase' })),

    // No digit
    fc.stringOf(
      fc.constantFrom(...(UPPERCASE + LOWERCASE + SPECIAL).split('')),
      { minLength: 8, maxLength: 20 }
    ).filter(s =>
      /[A-Z]/.test(s) && /[a-z]/.test(s) && /[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(s)
    ).map(password => ({ password, reason: 'no_digit' })),

    // No special character
    fc.stringOf(
      fc.constantFrom(...(UPPERCASE + LOWERCASE + DIGITS).split('')),
      { minLength: 8, maxLength: 20 }
    ).filter(s =>
      /[A-Z]/.test(s) && /[a-z]/.test(s) && /[0-9]/.test(s)
    ).map(password => ({ password, reason: 'no_special' })),

    // Empty password
    fc.constant({ password: '', reason: 'empty' }),
  );

/**
 * Generates a password of arbitrary length (for boundary testing).
 * Does not guarantee validity.
 */
export const arbPasswordOfLength = (length: number): fc.Arbitrary<string> =>
  fc.stringOf(
    fc.constantFrom(...(UPPERCASE + LOWERCASE + DIGITS + SPECIAL).split('')),
    { minLength: length, maxLength: length }
  );
