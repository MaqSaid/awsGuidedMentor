import { useState, useEffect, type ReactNode } from 'react';

/**
 * AdminRouteGuard — Protects admin routes by checking user role.
 * Redirects non-admin users to the dashboard with an access denied toast.
 *
 * Requirements: 31.1 (Super_Admin role access enforcement)
 */

interface AdminRouteGuardProps {
  children: ReactNode;
  /** Optional redirect path when user is not admin. Defaults to '/dashboard' */
  redirectTo?: string;
}

interface UserSession {
  role: string;
  isAdmin: boolean;
}

/**
 * Checks if the current user has admin privileges.
 * In production, this reads from the JWT claims or session context.
 */
async function checkAdminAccess(): Promise<{ isAdmin: boolean }> {
  try {
    const response = await fetch('/v1/users/me', {
      headers: { 'Content-Type': 'application/json' },
    });

    if (!response.ok) {
      return { isAdmin: false };
    }

    const data = (await response.json()) as UserSession;
    return { isAdmin: data.isAdmin === true };
  } catch {
    return { isAdmin: false };
  }
}

export function AdminRouteGuard({ children, redirectTo = '/dashboard' }: AdminRouteGuardProps) {
  const [state, setState] = useState<'loading' | 'authorized' | 'denied'>('loading');

  useEffect(() => {
    void verifyAccess();
  }, []);

  async function verifyAccess() {
    const { isAdmin } = await checkAdminAccess();

    if (isAdmin) {
      setState('authorized');
    } else {
      setState('denied');
      // Show access denied toast via custom event
      window.dispatchEvent(
        new CustomEvent('guided-mentor:toast', {
          detail: {
            type: 'error',
            message: 'Access denied. Administrator privileges required.',
          },
        })
      );

      // Redirect to dashboard
      window.location.href = redirectTo;
    }
  }

  if (state === 'loading') {
    return (
      <div
        className="flex items-center justify-center min-h-screen"
        style={{ background: 'var(--color-background)' }}
        role="status"
        aria-label="Verifying admin access"
      >
        <div className="flex flex-col items-center gap-4">
          <div
            className="w-8 h-8 border-2 border-t-transparent rounded-full animate-spin"
            style={{ borderColor: 'var(--color-primary)', borderTopColor: 'transparent' }}
            aria-hidden="true"
          />
          <p
            className="text-sm"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Verifying access...
          </p>
        </div>
      </div>
    );
  }

  if (state === 'denied') {
    // Return null while redirect happens
    return null;
  }

  return <>{children}</>;
}

/**
 * Higher-order component variant for class-based or simpler usage.
 */
export function withAdminGuard<P extends object>(
  WrappedComponent: React.ComponentType<P>,
  redirectTo = '/dashboard'
) {
  function AdminGuardedComponent(props: P) {
    return (
      <AdminRouteGuard redirectTo={redirectTo}>
        <WrappedComponent {...props} />
      </AdminRouteGuard>
    );
  }

  AdminGuardedComponent.displayName = `withAdminGuard(${
    WrappedComponent.displayName ?? WrappedComponent.name ?? 'Component'
  })`;

  return AdminGuardedComponent;
}

export default AdminRouteGuard;
