import { render, screen } from '@testing-library/react';
import { axe } from 'jest-axe';
import { describe, it, expect } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext, type AuthContextValue, type AuthUser } from '../providers/AuthProvider';
import { NavBar } from './NavBar';

function renderNavBar(authOverrides: Partial<AuthContextValue> = {}) {
  const defaultAuth: AuthContextValue = {
    accessToken: null,
    refreshToken: null,
    isAuthenticated: false,
    user: null,
    login: () => {},
    logout: () => {},
    refreshAccessToken: async () => false,
    updateUser: () => {},
    ...authOverrides,
  };

  return render(
    <MemoryRouter>
      <AuthContext value={defaultAuth}>
        <NavBar />
      </AuthContext>
    </MemoryRouter>
  );
}

const mockUser: AuthUser = {
  userId: 'user-1',
  email: 'alex@example.com',
  displayName: 'Alex Johnson',
  profilePhotoUrl: null,
  activeRole: 'mentee',
};

describe('NavBar', () => {
  it('renders without crashing', () => {
    renderNavBar();
    expect(screen.getByRole('navigation')).toBeInTheDocument();
  });

  it('has no accessibility violations when unauthenticated', async () => {
    const { container } = renderNavBar();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('has no accessibility violations when authenticated', async () => {
    const { container } = renderNavBar({
      isAuthenticated: true,
      user: mockUser,
    });
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('shows Sign In link when not authenticated', () => {
    renderNavBar();
    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shows user display name when authenticated', () => {
    renderNavBar({ isAuthenticated: true, user: mockUser });
    expect(screen.getByText('Alex Johnson')).toBeInTheDocument();
  });

  it('shows user initials avatar when authenticated', () => {
    renderNavBar({ isAuthenticated: true, user: mockUser });
    expect(screen.getByText('AJ')).toBeInTheDocument();
  });

  it('shows role badge when authenticated', () => {
    renderNavBar({ isAuthenticated: true, user: mockUser });
    expect(screen.getByText('Mentee')).toBeInTheDocument();
  });

  it('shows Mentor badge when user is a mentor', () => {
    renderNavBar({
      isAuthenticated: true,
      user: { ...mockUser, activeRole: 'mentor' },
    });
    const badges = screen.getAllByText('Mentor');
    // The word "Mentor" appears in logo and badge — the badge is the role indicator
    const roleBadge = badges.find((el) => el.className.includes('rounded-full'));
    expect(roleBadge).toBeInTheDocument();
  });

  it('has aria-label on the navigation element', () => {
    renderNavBar();
    expect(screen.getByRole('navigation')).toHaveAttribute('aria-label', 'Main navigation');
  });

  it('has aria-label on the notifications button', () => {
    renderNavBar({ isAuthenticated: true, user: mockUser });
    expect(screen.getByRole('button', { name: /notifications/i })).toBeInTheDocument();
  });
});
