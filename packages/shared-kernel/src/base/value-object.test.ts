import { describe, it, expect } from 'vitest';
import { ValueObject } from './value-object';

class Money extends ValueObject {
  constructor(
    public readonly amount: number,
    public readonly currency: string,
  ) {
    super();
  }

  protected getEqualityComponents(): unknown[] {
    return [this.amount, this.currency];
  }
}

class Address extends ValueObject {
  constructor(
    public readonly street: string,
    public readonly city: string,
  ) {
    super();
  }

  protected getEqualityComponents(): unknown[] {
    return [this.street, this.city];
  }
}

describe('ValueObject', () => {
  it('equals another value object with the same components', () => {
    const a = new Money(100, 'AUD');
    const b = new Money(100, 'AUD');
    expect(a.equals(b)).toBe(true);
  });

  it('is not equal when components differ', () => {
    const a = new Money(100, 'AUD');
    const b = new Money(200, 'AUD');
    expect(a.equals(b)).toBe(false);
  });

  it('is not equal to a different value object type', () => {
    const money = new Money(100, 'AUD');
    const address = new Address('100', 'AUD');
    expect(money.equals(address)).toBe(false);
  });

  it('is not equal to null or undefined', () => {
    const vo = new Money(50, 'USD');
    expect(vo.equals(null)).toBe(false);
    expect(vo.equals(undefined)).toBe(false);
  });

  it('provides a string representation', () => {
    const vo = new Money(99, 'AUD');
    expect(vo.toString()).toBe('Money(99, AUD)');
  });
});
