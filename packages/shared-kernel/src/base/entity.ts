import { IDomainEvent } from './domain-event';

/**
 * Base class for all domain entities.
 * Entities are identified by their unique ID and support domain event collection.
 *
 * @typeParam TId - The type of the entity's unique identifier (must be non-nullable)
 */
export abstract class Entity<TId> {
  private readonly _domainEvents: IDomainEvent[] = [];

  public readonly id: TId;

  protected constructor(id: TId) {
    this.id = id;
  }

  /** Read-only list of pending domain events */
  get domainEvents(): ReadonlyArray<IDomainEvent> {
    return [...this._domainEvents];
  }

  /** Add a domain event to the entity's event collection */
  addDomainEvent(domainEvent: IDomainEvent): void {
    this._domainEvents.push(domainEvent);
  }

  /** Clear all pending domain events (typically after publishing) */
  clearDomainEvents(): void {
    this._domainEvents.length = 0;
  }

  /** Entity equality is based on ID */
  equals(other: Entity<TId> | null | undefined): boolean {
    if (!other) return false;
    if (this === other) return true;
    return this.id === other.id;
  }
}
