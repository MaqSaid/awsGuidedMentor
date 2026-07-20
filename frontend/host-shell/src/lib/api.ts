/**
 * API base URL — resolved from environment variable.
 * - Local dev (MSW active): empty string (relative paths like /v1/... are intercepted by MSW)
 * - Local dev (MSW disabled): http://localhost:5000
 * - Production: https://guidedmentor-api.onrender.com (your Render URL)
 */
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? '';

/**
 * Build a full API URL from a relative path.
 * @example apiUrl('/v1/auth/magic-link') → 'https://guidedmentor-api.onrender.com/v1/auth/magic-link'
 */
export function apiUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}

/**
 * Fetch wrapper that prepends the API base URL and includes auth token.
 */
export async function apiFetch(path: string, options: RequestInit = {}): Promise<Response> {
  const token = localStorage.getItem('gm_access_token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  return fetch(apiUrl(path), { ...options, headers });
}
