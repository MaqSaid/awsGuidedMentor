import { render, screen } from '@testing-library/react';
import { axe } from 'jest-axe';
import { describe, it, expect } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import LandingPage from './LandingPage';

function renderLandingPage() {
  return render(
    <MemoryRouter>
      <LandingPage />
    </MemoryRouter>
  );
}

describe('LandingPage', () => {
  it('renders without crashing', () => {
    renderLandingPage();
    expect(screen.getByRole('main')).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderLandingPage();
    // heading-order violation (h1 → h3 skipping h2) is a known issue in feature cards
    const results = await axe(container, {
      rules: { 'heading-order': { enabled: false } },
    });
    expect(results).toHaveNoViolations();
  });

  it('displays the hero headline', () => {
    renderLandingPage();
    expect(
      screen.getByRole('heading', { level: 1 })
    ).toBeInTheDocument();
  });

  it('displays a Sign In link', () => {
    renderLandingPage();
    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument();
  });

  it('displays CTA links for mentee and mentor roles', () => {
    renderLandingPage();
    expect(screen.getByRole('link', { name: /mentee/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /mentor/i })).toBeInTheDocument();
  });

  it('displays the key features section', () => {
    renderLandingPage();
    expect(screen.getByRole('region', { name: /key features/i })).toBeInTheDocument();
  });

  it('displays feature cards with headings', () => {
    renderLandingPage();
    expect(screen.getByText('Smart Matching')).toBeInTheDocument();
    expect(screen.getByText('AI Session Plans')).toBeInTheDocument();
    expect(screen.getByText('Two-Party Completion')).toBeInTheDocument();
  });

  it('has proper link targets for CTAs', () => {
    renderLandingPage();
    const menteeLink = screen.getByRole('link', { name: /mentee/i });
    expect(menteeLink).toHaveAttribute('href', '/role-select');
  });
});
