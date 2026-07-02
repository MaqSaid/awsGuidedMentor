import { render } from '@testing-library/react';
import { axe } from 'jest-axe';
import { describe, it, expect } from 'vitest';
import { Skeleton, DashboardSkeleton, BrowseSkeleton, SessionPlanSkeleton } from './Skeleton';

describe('Skeleton', () => {
  it('renders without crashing', () => {
    const { container } = render(<Skeleton />);
    expect(container.firstChild).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Skeleton />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('renders the specified number of skeleton elements', () => {
    const { container } = render(<Skeleton count={3} />);
    const skeletons = container.querySelectorAll('[aria-hidden="true"]');
    expect(skeletons).toHaveLength(3);
  });

  it('applies aria-hidden to skeleton elements', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('[aria-hidden]');
    expect(skeleton).toHaveAttribute('aria-hidden', 'true');
  });

  it('applies text variant class by default', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.firstChild as HTMLElement;
    expect(skeleton.className).toContain('h-4');
    expect(skeleton.className).toContain('w-full');
  });

  it('applies circle variant class', () => {
    const { container } = render(<Skeleton variant="circle" />);
    const skeleton = container.firstChild as HTMLElement;
    expect(skeleton.className).toContain('rounded-full');
  });

  it('applies custom className', () => {
    const { container } = render(<Skeleton className="h-10 w-48" />);
    const skeleton = container.firstChild as HTMLElement;
    expect(skeleton.className).toContain('h-10');
    expect(skeleton.className).toContain('w-48');
  });
});

describe('DashboardSkeleton', () => {
  it('renders without crashing', () => {
    const { container } = render(<DashboardSkeleton />);
    expect(container.firstChild).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<DashboardSkeleton />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});

describe('BrowseSkeleton', () => {
  it('renders without crashing', () => {
    const { container } = render(<BrowseSkeleton />);
    expect(container.firstChild).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<BrowseSkeleton />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});

describe('SessionPlanSkeleton', () => {
  it('renders without crashing', () => {
    const { container } = render(<SessionPlanSkeleton />);
    expect(container.firstChild).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<SessionPlanSkeleton />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
