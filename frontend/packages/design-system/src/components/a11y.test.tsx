/**
 * Accessibility tests using axe-core for all design system components.
 * Verifies WCAG 2.1 AA compliance.
 */
import { render } from '@testing-library/react';
import { configureAxe, toHaveNoViolations } from 'jest-axe';
import { describe, it, expect } from 'vitest';
import { EmptyState } from './EmptyState';
import { ErrorMessage } from './ErrorMessage';
import { Tooltip } from './Tooltip';
import { Button } from './Button';

expect.extend(toHaveNoViolations);

const axe = configureAxe({
  rules: {
    // Disable color-contrast in JSDOM (can't compute styles accurately)
    'color-contrast': { enabled: false },
  },
});

describe('Accessibility — Design System Components', () => {
  it('Button has no a11y violations', async () => {
    const { container } = render(<Button variant="primary">Click me</Button>);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('Button (disabled) has no a11y violations', async () => {
    const { container } = render(<Button variant="primary" disabled>Disabled</Button>);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('EmptyState has no a11y violations', async () => {
    const { container } = render(
      <EmptyState
        title="No sessions"
        message="Browse mentors to start a new session."
        actionLabel="Browse Mentors"
        actionHref="/browse"
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('ErrorMessage (block) has no a11y violations', async () => {
    const { container } = render(
      <ErrorMessage
        message="Something went wrong."
        onRetry={() => {}}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('ErrorMessage (inline) has no a11y violations', async () => {
    const { container } = render(
      <ErrorMessage
        variant="inline"
        message="Field error."
        onRetry={() => {}}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('Tooltip has no a11y violations', async () => {
    const { container } = render(
      <Tooltip content="This field requires 12+ characters">
        <button>Info</button>
      </Tooltip>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
