/**
 * Result type for operation outcomes.
 * Provides a functional approach to error handling without exceptions.
 * Follows the Result pattern common in DDD.
 */

/**
 * Result<T> represents the outcome of an operation that may produce a value.
 */
export class Result<T = void> {
  public readonly isSuccess: boolean;
  public readonly isFailure: boolean;
  public readonly value: T | undefined;
  public readonly error: string | undefined;

  private constructor(isSuccess: boolean, value?: T, error?: string) {
    this.isSuccess = isSuccess;
    this.isFailure = !isSuccess;
    this.value = value;
    this.error = error;
  }

  /** Create a successful result with a value */
  static success<T>(value: T): Result<T> {
    return new Result<T>(true, value, undefined);
  }

  /** Create a successful result with no value (void operations) */
  static ok(): Result<void> {
    return new Result<void>(true, undefined, undefined);
  }

  /** Create a failed result with an error message */
  static failure<T = void>(error: string): Result<T> {
    return new Result<T>(false, undefined, error);
  }

  /**
   * Get the value or throw if the result is a failure.
   * Use only when you're certain the operation succeeded.
   */
  getValueOrThrow(): T {
    if (this.isFailure) {
      throw new Error(`Cannot access value of a failed result: ${this.error}`);
    }
    return this.value as T;
  }

  /** Map the value to a new type if successful, propagate failure otherwise */
  map<U>(fn: (value: T) => U): Result<U> {
    if (this.isFailure) {
      return Result.failure<U>(this.error!);
    }
    return Result.success(fn(this.value as T));
  }
}
