import { Entity } from './entity';

/**
 * Base class for aggregate roots.
 * Aggregate roots are the consistency boundaries of the domain model.
 * They are the only entities that repositories operate on directly.
 *
 * @typeParam TId - The type of the aggregate root's unique identifier
 */
export abstract class AggregateRoot<TId> extends Entity<TId> {
  protected constructor(id: TId) {
    super(id);
  }
}
