import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { Checklist } from './Checklist';

const defaultProps = {
  title: 'Pre-work',
  items: ['Read chapter 1', 'Watch intro video', 'Complete quiz'],
  checkedState: [false, true, false],
  type: 'prework' as const,
  onToggle: vi.fn().mockResolvedValue(undefined),
};

describe('Checklist', () => {
  it('renders all items with correct checked state', () => {
    render(<Checklist {...defaultProps} />);

    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes).toHaveLength(3);
    expect(checkboxes[0]).not.toBeChecked();
    expect(checkboxes[1]).toBeChecked();
    expect(checkboxes[2]).not.toBeChecked();
  });

  it('renders the section title', () => {
    render(<Checklist {...defaultProps} />);
    expect(screen.getByText('Pre-work')).toBeInTheDocument();
  });

  it('calls onToggle with correct update when item is checked', async () => {
    const user = userEvent.setup();
    const onToggle = vi.fn().mockResolvedValue(undefined);
    render(<Checklist {...defaultProps} onToggle={onToggle} />);

    const checkboxes = screen.getAllByRole('checkbox');
    await user.click(checkboxes[0]!);

    expect(onToggle).toHaveBeenCalledWith({
      type: 'prework',
      index: 0,
      checked: true,
    });
  });

  it('optimistically updates checkbox before server responds', async () => {
    const user = userEvent.setup();
    // Never-resolving promise to simulate pending state
    const onToggle = vi.fn().mockReturnValue(new Promise(() => {}));
    render(<Checklist {...defaultProps} onToggle={onToggle} />);

    const checkboxes = screen.getAllByRole('checkbox');
    await user.click(checkboxes[0]!);

    // Should be visually checked immediately (optimistic)
    expect(checkboxes[0]).toBeChecked();
  });

  it('reverts checkbox on failure and shows retry button', async () => {
    const user = userEvent.setup();
    const onToggle = vi.fn().mockRejectedValue(new Error('Network error'));
    render(<Checklist {...defaultProps} onToggle={onToggle} />);

    const checkboxes = screen.getAllByRole('checkbox');
    await user.click(checkboxes[0]!);

    // Wait for the revert
    await waitFor(() => {
      expect(checkboxes[0]).not.toBeChecked();
    });

    // Should show error and retry button
    expect(screen.getByText('Failed to save. Please try again.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('retry button re-triggers the toggle', async () => {
    const user = userEvent.setup();
    const onToggle = vi
      .fn()
      .mockRejectedValueOnce(new Error('fail'))
      .mockResolvedValueOnce(undefined);
    render(<Checklist {...defaultProps} onToggle={onToggle} />);

    const checkboxes = screen.getAllByRole('checkbox');
    await user.click(checkboxes[0]!);

    // Wait for error
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });

    // Click retry
    await user.click(screen.getByRole('button', { name: /retry/i }));

    expect(onToggle).toHaveBeenCalledTimes(2);
  });

  it('has accessible aria labels on checkboxes', () => {
    render(<Checklist {...defaultProps} />);

    expect(
      screen.getByRole('checkbox', { name: /check: read chapter 1/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('checkbox', { name: /uncheck: watch intro video/i })
    ).toBeInTheDocument();
  });
});
