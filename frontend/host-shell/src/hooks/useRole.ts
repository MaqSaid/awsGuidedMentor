import { useContext } from 'react';
import { RoleContext, type RoleContextValue } from '../providers/RoleProvider';

/**
 * Typed hook wrapping RoleProvider context.
 * Throws if used outside RoleProvider.
 *
 * Requirements: 2.4, 2.7
 */
export function useRole(): RoleContextValue {
  const context = useContext(RoleContext);
  if (!context) {
    throw new Error('useRole must be used within a RoleProvider');
  }
  return context;
}
