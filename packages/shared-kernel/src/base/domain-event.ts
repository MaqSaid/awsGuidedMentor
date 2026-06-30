/**
 * Domain event marker interface.
 * All domain events implement this interface and carry the timestamp
 * at which the event occurred.
 */
export interface IDomainEvent {
  /** Timestamp when the domain event occurred */
  readonly occurredAt: Date;
}

/**
 * Base class for domain events providing default timestamp initialization.
 */
export abstract class DomainEvent implements IDomainEvent {
  public readonly occurredAt: Date;

  protected constructor(occurredAt?: Date) {
    this.occurredAt = occurredAt ?? new Date();
  }
}
