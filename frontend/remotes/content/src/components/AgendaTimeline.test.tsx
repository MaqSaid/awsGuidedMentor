import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { AgendaTimeline } from './AgendaTimeline';

const mockItems = [
  { title: 'Intro & Goals', durationMinutes: 5, description: 'Set the stage' },
  { title: 'Deep Dive', durationMinutes: 20, description: 'Core topic discussion' },
  { title: 'Q&A', durationMinutes: 10, description: 'Open questions' },
];

describe('AgendaTimeline', () => {
  it('renders all agenda items', () => {
    render(<AgendaTimeline items={mockItems} />);

    expect(screen.getByText('Intro & Goals')).toBeInTheDocument();
    expect(screen.getByText('Deep Dive')).toBeInTheDocument();
    expect(screen.getByText('Q&A')).toBeInTheDocument();
  });

  it('displays duration labels', () => {
    render(<AgendaTimeline items={mockItems} />);

    expect(screen.getByText('5 min')).toBeInTheDocument();
    expect(screen.getByText('20 min')).toBeInTheDocument();
    expect(screen.getByText('10 min')).toBeInTheDocument();
  });

  it('displays descriptions', () => {
    render(<AgendaTimeline items={mockItems} />);

    expect(screen.getByText('Set the stage')).toBeInTheDocument();
    expect(screen.getByText('Core topic discussion')).toBeInTheDocument();
    expect(screen.getByText('Open questions')).toBeInTheDocument();
  });

  it('renders as an ordered list', () => {
    render(<AgendaTimeline items={mockItems} />);

    expect(screen.getByRole('list')).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(3);
  });

  it('has accessible section label', () => {
    render(<AgendaTimeline items={mockItems} />);

    expect(screen.getByRole('region', { name: /session agenda/i })).toBeInTheDocument();
  });

  it('renders empty when no items provided', () => {
    render(<AgendaTimeline items={[]} />);

    expect(screen.getByRole('list')).toBeInTheDocument();
    expect(screen.queryAllByRole('listitem')).toHaveLength(0);
  });
});
