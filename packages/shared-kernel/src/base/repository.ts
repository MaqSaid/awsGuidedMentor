import { AggregateRoot } from './aggregate-root';

/**
 * Generic repository interface for aggregate roots.
 * Defines the standard persistence operations for domain aggregates.
 *
 * @typeParam T - The aggregate root type
 * @typeParam TId - The aggregate root's identifier type
 */
export interface IRepository<T extends AggregateRoot<TId>, TId = string> {
  /** Find an aggregate by its unique identifier */
  findById(id: TId): Promise<T | null>;

  /** Persist a new or updated aggregate */
  save(aggregate: T): Promise<void>;

  /** Remove an aggregate from persistence */
  delete(id: TId): Promise<void>;

  /** Check whether an aggregate with the given ID exists */
  exists(id: TId): Promise<boolean>;
}
