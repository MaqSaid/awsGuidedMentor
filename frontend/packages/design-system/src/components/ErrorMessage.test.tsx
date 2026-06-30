import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ErrorMessage } from './ErrorMessage';

describe('ErrorMessage', () => {
  it('renders default error message', () => {
    render(<ErrorMessage />);
    expect(screen.getByText('Something went wrong. Please try again.')).toBeInTheDocument();
  });

  it('renders custom error message', () => {
    render(<ErrorMessage message="Unable to load sessions." />);
    expect(screen.getByText('Unable to load sessions.')).toBeInTheDocument();
  });

  it('renders retry button when onRetry is provided', () => {
    const onRetry = vi.fn();
    render(<ErrorMessage onRetry={onRetry} />);
    const button = screen.getByText('Try Again');
    fireEvent.click(button);
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('shows "Retrying..." text when isRetrying is true', () => {
    render(<ErrorMessage onRetry={() => {}} isRetrying={true} />);
    expect(screen.getByText('Retrying...')).toBeInTheDocument();
  });

  it('renders "Learn more" link when provided', () => {
    render(<ErrorMessage learnMoreHref="https://help.example.com" />);
    const link = screen.getByText('Learn more');
    expect(link).toHaveAttribute('href', 'https://help.example.com');
    expect(link).toHaveAttribute('target', '_blank');
  });

  it('has accessible role="alert"', () => {
    render(<ErrorMessage />);
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('renders inline variant compactly', () => {
    const onRetry = vi.fn();
    render(<ErrorMessage variant="inline" onRetry={onRetry} message="Retry available" />);
    expect(screen.getByText('Retry available')).toBeInTheDocument();
    const retryButton = screen.getByText('Retry');
    fireEvent.click(retryButton);
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('does not render retry button when onRetry is not provided', () => {
    render(<ErrorMessage />);
    expect(screen.queryByText('Try Again')).not.toBeInTheDocument();
    expect(screen.queryByText('Retry')).not.toBeInTheDocument();
  });
});
