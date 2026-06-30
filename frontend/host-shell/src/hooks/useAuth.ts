import { useContext } from 'react';
import { AuthContext, type AuthContextValue } from '../providers/AuthProvider';

/**
 * Typed hook wrapping AuthProvider context.
 * Throws if used outside AuthProvider.
 *
 * Requirements: 18.2
 */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
