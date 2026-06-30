import { test, expect } from '../fixtures/auth';

test.describe('Session Plan Journey', () => {
  test.skip('should display generated session plan', async ({ menteePage }) => {
    // Placeholder — implement when Content context frontend is complete
    await menteePage.goto('/sessions/test-session-id/plan');
    await expect(menteePage.getByRole('heading', { name: /session plan/i })).toBeVisible();
  });
});
