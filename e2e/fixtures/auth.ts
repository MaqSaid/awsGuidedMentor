import { test as base, type Page } from '@playwright/test';

/**
 * Mock JWT token for E2E testing with authenticated sessions.
 * In staging, this uses a real Cognito test user; locally it mocks the auth state.
 */
export interface AuthFixtures {
  authenticatedPage: Page;
  mentorPage: Page;
  menteePage: Page;
}

/**
 * Creates a mock JWT for local/staging testing.
 * Replace with real Cognito tokens when testing against staging environment.
 */
function createMockJwt(role: 'mentor' | 'mentee'): string {
  const header = btoa(JSON.stringify({ alg: 'RS256', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({
      sub: `test-${role}-user-id`,
      email: `test.${role}@guidedmentor.dev`,
      'custom:role': role,
      'custom:chapter': 'Sydney',
      exp: Math.floor(Date.now() / 1000) + 900, // 15 min
      iss: 'https://cognito-idp.ap-southeast-2.amazonaws.com/test-pool',
    })
  );
  const signature = btoa('mock-signature');
  return `${header}.${payload}.${signature}`;
}

export const test = base.extend<AuthFixtures>({
  authenticatedPage: async ({ page }, use) => {
    // Set auth tokens in localStorage before navigating
    await page.addInitScript(() => {
      window.localStorage.setItem(
        'auth_tokens',
        JSON.stringify({
          accessToken: 'mock-access-token',
          refreshToken: 'mock-refresh-token',
          expiresAt: Date.now() + 900_000,
        })
      );
    });
    await use(page);
  },

  mentorPage: async ({ page }, use) => {
    const token = createMockJwt('mentor');
    await page.addInitScript((jwt) => {
      window.localStorage.setItem(
        'auth_tokens',
        JSON.stringify({
          accessToken: jwt,
          refreshToken: 'mock-refresh-token',
          expiresAt: Date.now() + 900_000,
        })
      );
      window.localStorage.setItem('active_role', 'mentor');
    }, token);
    await use(page);
  },

  menteePage: async ({ page }, use) => {
    const token = createMockJwt('mentee');
    await page.addInitScript((jwt) => {
      window.localStorage.setItem(
        'auth_tokens',
        JSON.stringify({
          accessToken: jwt,
          refreshToken: 'mock-refresh-token',
          expiresAt: Date.now() + 900_000,
        })
      );
      window.localStorage.setItem('active_role', 'mentee');
    }, token);
    await use(page);
  },
});

export { expect } from '@playwright/test';
