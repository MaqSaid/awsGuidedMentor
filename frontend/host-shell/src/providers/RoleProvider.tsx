import {
  createContext,
  useCallback,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { AuthContext, type Role } from './AuthProvider';
import { useContext } from 'react';
import { apiUrl } from '../lib/api';

/**
 * RoleProvider — Active role context with optimistic toggle.
 * Calls API to persist role change, rolls back on failure.
 *
 * Requirements: 2.4, 2.7, 2.9
 */

export interface RoleContextValue {
  activeRole: Role | null;
  isToggling: boolean;
  toggleRole: () => Promise<boolean>;
}

export const RoleContext = createContext<RoleContextValue | null>(null);

interface RoleProviderProps {
  children: ReactNode;
}

export function RoleProvider({ children }: RoleProviderProps) {
  const auth = useContext(AuthContext);
  const [isToggling, setIsToggling] = useState(false);

  const activeRole = auth?.user?.activeRole ?? null;

  const toggleRole = useCallback(async (): Promise<boolean> => {
    if (!auth || !auth.user || !auth.accessToken) return false;
    if (isToggling) return false;

    const previousRole = auth.user.activeRole;
    const newRole: Role = previousRole === 'mentor' ? 'mentee' : 'mentor';

    // Optimistic update
    setIsToggling(true);
    auth.updateUser({ activeRole: newRole });

    try {
      const response = await fetch(apiUrl('/v1/users/toggle-role'), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${auth.accessToken}`,
        },
      });

      if (!response.ok) {
        // Revert on failure
        auth.updateUser({ activeRole: previousRole });
        return false;
      }

      return true;
    } catch {
      // Revert on failure
      auth.updateUser({ activeRole: previousRole });
      return false;
    } finally {
      setIsToggling(false);
    }
  }, [auth, isToggling]);

  const value = useMemo<RoleContextValue>(
    () => ({
      activeRole,
      isToggling,
      toggleRole,
    }),
    [activeRole, isToggling, toggleRole]
  );

  return <RoleContext value={value}>{children}</RoleContext>;
}
