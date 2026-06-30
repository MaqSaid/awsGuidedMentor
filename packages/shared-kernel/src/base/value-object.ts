/**
 * Base class for value objects.
 * Value objects are defined by their attributes rather than by identity.
 * Equality is determined by structural comparison of all properties.
 */
export abstract class ValueObject {
  /**
   * Returns the properties that participate in equality comparison.
   * Subclasses must implement this to define their equality semantics.
   */
  protected abstract getEqualityComponents(): unknown[];

  /** Structural equality based on equality components */
  equals(other: ValueObject | null | undefined): boolean {
    if (!other) return false;
    if (this === other) return true;
    if (this.constructor !== other.constructor) return false;

    const thisComponents = this.getEqualityComponents();
    const otherComponents = other.getEqualityComponents();

    if (thisComponents.length !== otherComponents.length) return false;

    return thisComponents.every((component, index) => {
      const otherComponent = otherComponents[index];
      if (component === null && otherComponent === null) return true;
      if (component === undefined && otherComponent === undefined) return true;
      if (component === null || component === undefined) return false;
      if (otherComponent === null || otherComponent === undefined) return false;
      return component === otherComponent;
    });
  }

  /** String representation for debugging */
  toString(): string {
    return `${this.constructor.name}(${this.getEqualityComponents().join(', ')})`;
  }
}
