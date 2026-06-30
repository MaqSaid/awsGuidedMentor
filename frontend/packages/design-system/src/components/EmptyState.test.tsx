import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { EmptyState } from './EmptyState';

describe('EmptyState', () => {
  it('renders message text', () => {
    render(<EmptyState message="No sessions yet." />);
    expect(screen.getByText('No sessions yet.')).toBeInTheDocument();
  });

  it('renders optional title', () => {
    render(<EmptyState title="Get Started" message="No sessions yet." />);
    expect(screen.getByText('Get Started')).toBeInTheDocument();
  });

  it('renders primary action as link when actionHref is provided', () => {
    render(
      <EmptyState
        message="No mentors found."
        actionLabel="Browse Mentors"
        actionHref="/browse"
      />
    );
    const link = screen.getByText('Browse Mentors');
    expect(link).toBeInTheDocument();
    expect(link.closest('a')).toHaveAttribute('href', '/browse');
  });

  it('renders primary action as button when onAction is provided', () => {
    const onAction = vi.fn();
    render(
      <EmptyState
        message="No results."
        actionLabel="Try Again"
        onAction={onAction}
      />
    );
    const button = screen.getByText('Try Again');
    fireEvent.click(button);
    expect(onAction).toHaveBeenCalledTimes(1);
  });

  it('renders secondary action button', () => {
    const onSecondary = vi.fn();
    render(
      <EmptyState
        message="Nothing here."
        actionLabel="Create"
        onAction={() => {}}
        secondaryLabel="Learn More"
        onSecondaryAction={onSecondary}
      />
    );
    const button = screen.getByText('Learn More');
    fireEvent.click(button);
    expect(onSecondary).toHaveBeenCalledTimes(1);
  });

  it('renders icon when provided', () => {
    render(
      <EmptyState
        message="Empty."
        icon={<span data-testid="custom-icon">📭</span>}
      />
    );
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
  });

  it('has accessible role="status"', () => {
    render(<EmptyState message="No data." />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });
});
