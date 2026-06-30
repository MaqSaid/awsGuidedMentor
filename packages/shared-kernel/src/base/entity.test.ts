import { describe, it, expect } from 'vitest';
import { Entity } from './entity';
import { DomainEvent } from './domain-event';

class TestEvent extends DomainEvent {
  constructor(public readonly payload: string) {
    super();
  }
}

class TestEntity extends Entity<string> {
  constructor(id: string) {
    super(id);
  }
}

describe('Entity', () => {
  it('stores the provided id', () => {
    const entity = new TestEntity('test-id-1');
    expect(entity.id).toBe('test-id-1');
  });

  it('starts with empty domain events', () => {
    const entity = new TestEntity('test-id');
    expect(entity.domainEvents).toHaveLength(0);
  });

  it('collects domain events', () => {
    const entity = new TestEntity('test-id');
    const event = new TestEvent('payload-1');
    entity.addDomainEvent(event);
    expect(entity.domainEvents).toHaveLength(1);
    expect(entity.domainEvents[0]).toBe(event);
  });

  it('clears domain events', () => {
    const entity = new TestEntity('test-id');
    entity.addDomainEvent(new TestEvent('a'));
    entity.addDomainEvent(new TestEvent('b'));
    entity.clearDomainEvents();
    expect(entity.domainEvents).toHaveLength(0);
  });

  it('returns a defensive copy of domain events', () => {
    const entity = new TestEntity('test-id');
    entity.addDomainEvent(new TestEvent('a'));
    const events = entity.domainEvents;
    // Mutating the returned array should not affect the internal state
    (events as unknown[]).push(new TestEvent('mutated'));
    expect(entity.domainEvents).toHaveLength(1);
  });

  it('equals another entity with the same id', () => {
    const a = new TestEntity('same-id');
    const b = new TestEntity('same-id');
    expect(a.equals(b)).toBe(true);
  });

  it('is not equal to an entity with a different id', () => {
    const a = new TestEntity('id-1');
    const b = new TestEntity('id-2');
    expect(a.equals(b)).toBe(false);
  });

  it('is not equal to null or undefined', () => {
    const entity = new TestEntity('test-id');
    expect(entity.equals(null)).toBe(false);
    expect(entity.equals(undefined)).toBe(false);
  });
});
