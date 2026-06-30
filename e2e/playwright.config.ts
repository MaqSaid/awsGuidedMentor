import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration for GuidedMentor.
 * Targets staging environment by default, chromium only.
 */
export default defineConfig({
  testDir: './journeys',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html', { outputFolder: '../playwright-report' }],
    ...(process.env.CI ? [['junit' as const, { outputFile: '../test-results/e2e-results.xml' }]] : []),
  ],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  /* Run local dev server before tests if not in CI */
  webServer: process.env.CI
    ? undefined
    : {
        command: 'npm run dev',
        cwd: '../frontend',
        url: 'http://localhost:3000',
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
      },
});
