import { render, screen } from '@testing-library/react';
import { axe } from 'jest-axe';
import { describe, it, expect } from 'vitest';
import { ScoreRing } from './ScoreRing';

describe('ScoreRing', () => {
  it('renders without crashing', () => {
    render(<ScoreRing score={75} />);
    expect(screen.getByRole('img')).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<ScoreRing score={85} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('displays the score value', () => {
    render(<ScoreRing score={92} />);
    expect(screen.getByText('92')).toBeInTheDocument();
  });

  it('has descriptive aria-label with score percentage', () => {
    render(<ScoreRing score={78} />);
    expect(screen.getByRole('img')).toHaveAttribute(
      'aria-label',
      'Compatibility score: 78%'
    );
  });

  it('renders at small size', () => {
    render(<ScoreRing score={60} size="sm" />);
    const ring = screen.getByRole('img');
    expect(ring).toHaveStyle({ width: '48px', height: '48px' });
  });

  it('renders at medium size by default', () => {
    render(<ScoreRing score={60} />);
    const ring = screen.getByRole('img');
    expect(ring).toHaveStyle({ width: '72px', height: '72px' });
  });

  it('renders at large size', () => {
    render(<ScoreRing score={60} size="lg" />);
    const ring = screen.getByRole('img');
    expect(ring).toHaveStyle({ width: '120px', height: '120px' });
  });

  it('uses mint color for scores above 80', () => {
    render(<ScoreRing score={85} />);
    const scoreText = screen.getByText('85');
    expect(scoreText.className).toContain('text-mint');
  });

  it('uses violet color for scores between 60 and 80', () => {
    render(<ScoreRing score={70} />);
    const scoreText = screen.getByText('70');
    expect(scoreText.className).toContain('text-violet-light');
  });

  it('uses amber color for scores below 60', () => {
    render(<ScoreRing score={45} />);
    const scoreText = screen.getByText('45');
    expect(scoreText.className).toContain('text-amber');
  });
});
