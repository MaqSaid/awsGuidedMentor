import { test, expect } from '../fixtures/auth';

test.describe('Mentoring Journey', () => {
  test.skip('should display browse page with mentor cards', async ({ menteePage }) => {
    // Placeholder — implement when Mentoring frontend is complete
    await menteePage.goto('/mentoring/browse');
    await expect(menteePage.getByRole('heading', { name: /browse mentors/i })).toBeVisible();
  });

  test.skip('should allow mentee to lock a mentor', async ({ menteePage }) => {
    // Placeholder — implement when Locking mechanism is complete
    await menteePage.goto('/mentoring/browse');
  });
});
