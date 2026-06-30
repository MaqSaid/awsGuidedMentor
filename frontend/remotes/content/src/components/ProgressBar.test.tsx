import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ProgressBar, calculateProgress } from './ProgressBar';

describe('calculateProgress', () => {
  it('returns 0 when totalCount is 0', () => {
    expect(calculateProgress(0, 0)).toBe(0);
  });

  it('returns 0 when no items are checked', () => {
    expect(calculateProgress(0, 5)).toBe(0);
  });

  it('returns 100 when all items are checked', () => {
    expect(calculateProgress(5, 5)).toBe(100);
  });

  it('rounds to the nearest integer', () => {
    // 1/3 = 0.333... → 33%
    expect(calculateProgress(1, 3)).toBe(33);
    // 2/3 = 0.666... → 67%
    expect(calculateProgress(2, 3)).toBe(67);
  });

  it('calculates correct percentage for typical cases', () => {
    expect(calculateProgress(3, 6)).toBe(50);
    expect(calculateProgress(7, 10)).toBe(70);
  });
});

describe('ProgressBar', () => {
  it('renders the progress bar with correct percentage', () => {
    render(<ProgressBar checkedCount={3} totalCount={6} />);

    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '50');
    expect(screen.getByText('50%')).toBeInTheDocument();
    expect(screen.getByText('3 of 6 items completed')).toBeInTheDocument();
  });

  it('renders 0% when no items are checked', () => {
    render(<ProgressBar checkedCount={0} totalCount={4} />);

    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '0');
    expect(screen.getByText('0%')).toBeInTheDocument();
  });

  it('renders 0% when total is 0', () => {
    render(<ProgressBar checkedCount={0} totalCount={0} />);

    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '0');
    expect(screen.getByText('0%')).toBeInTheDocument();
  });

  it('has accessible progress bar label', () => {
    render(<ProgressBar checkedCount={2} totalCount={8} />);

    expect(screen.getByRole('progressbar')).toHaveAttribute(
      'aria-label',
      'Checklist progress: 25% complete'
    );
  });
});
