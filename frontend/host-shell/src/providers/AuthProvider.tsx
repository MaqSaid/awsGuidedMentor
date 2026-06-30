import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

/**
 * AuthProvider — JWT state management with silent refresh.
 * Stores access/refresh tokens, validates expiry, and handles refresh flow.
 *
 * Requirements: 1.10, 2.4, 18.2
 */

export type Role = 'mentor' | 'mentee';

export interface AuthUser {
  userId: string;
  email: string;
  displayName: string;
  profilePhotoUrl: string | null;
  activeRole: Role | null;
}

export interface AuthContextValue {
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  user: AuthUser | null;
  login: (accessToken: string, refreshToken: string, user: AuthUser) => void;
  logout: () => void;
  refreshAccessToken: () => Promise<boolean>;
  updateUser: (updates: Partial<AuthUser>) => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

const TOKEN_STORAGE_KEY = 'gm_access_token';
const REFRESH_STORAGE_KEY = 'gm_refresh_token';
const USER_STORAGE_KEY = 'gm_user';

function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]!));
    const exp = payload.exp as number;
    // Consider expired 30 seconds before actual expiry for buffer
    return Date.now() >= (exp - 30) * 1000;
  } catch {
    return true;
  }
}

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [accessToken, setAccessToken] = useState<string | null>(() =>
    localStorage.getItem(TOKEN_STORAGE_KEY)
  );
  const [refreshToken, setRefreshToken] = useState<string | null>(() =>
    localStorage.getItem(REFRESH_STORAGE_KEY)
  );
  const [user, setUser] = useState<AuthUser | null>(() => {
    const stored = localStorage.getItem(USER_STORAGE_KEY);
    if (stored) {
      try {
        return JSON.parse(stored) as AuthUser;
      } catch {
        return null;
      }
    }
    return null;
  });

  const isAuthenticated = useMemo(() => {
    if (!accessToken) return false;
    return !isTokenExpired(accessToken);
  }, [accessToken]);

  const login = useCallback(
    (newAccessToken: string, newRefreshToken: string, newUser: AuthUser) => {
      setAccessToken(newAccessToken);
      setRefreshToken(newRefreshToken);
      setUser(newUser);
      localStorage.setItem(TOKEN_STORAGE_KEY, newAccessToken);
      localStorage.setItem(REFRESH_STORAGE_KEY, newRefreshToken);
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(newUser));
    },
    []
  );

  const logout = useCallback(() => {
    setAccessToken(null);
    setRefreshToken(null);
    setUser(null);
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(REFRESH_STORAGE_KEY);
    localStorage.removeItem(USER_STORAGE_KEY);
  }, []);

  const refreshAccessToken = useCallback(async (): Promise<boolean> => {
    const currentRefresh = refreshToken ?? localStorage.getItem(REFRESH_STORAGE_KEY);
    if (!currentRefresh) {
      logout();
      return false;
    }

    try {
      const response = await fetch('/v1/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: currentRefresh }),
      });

      if (!response.ok) {
        logout();
        return false;
      }

      const data = (await response.json()) as {
        accessToken: string;
        refreshToken: string;
      };

      setAccessToken(data.accessToken);
      setRefreshToken(data.refreshToken);
      localStorage.setItem(TOKEN_STORAGE_KEY, data.accessToken);
      localStorage.setItem(REFRESH_STORAGE_KEY, data.refreshToken);
      return true;
    } catch {
      logout();
      return false;
    }
  }, [refreshToken, logout]);

  const updateUser = useCallback((updates: Partial<AuthUser>) => {
    setUser((prev) => {
      if (!prev) return prev;
      const updated = { ...prev, ...updates };
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(updated));
      return updated;
    });
  }, []);

  // Silent refresh: set up interval to refresh before token expires
  useEffect(() => {
    if (!accessToken || !refreshToken) return;

    // Check every 60 seconds if token needs refresh
    const interval = setInterval(() => {
      if (accessToken && isTokenExpired(accessToken)) {
        void refreshAccessToken();
      }
    }, 60_000);

    return () => clearInterval(interval);
  }, [accessToken, refreshToken, refreshAccessToken]);

  // On mount: validate stored token
  useEffect(() => {
    if (accessToken && isTokenExpired(accessToken)) {
      void refreshAccessToken();
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const value = useMemo<AuthContextValue>(
    () => ({
      accessToken,
      refreshToken,
      isAuthenticated,
      user,
      login,
      logout,
      refreshAccessToken,
      updateUser,
    }),
    [accessToken, refreshToken, isAuthenticated, user, login, logout, refreshAccessToken, updateUser]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
