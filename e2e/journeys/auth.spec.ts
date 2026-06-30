import { test, expect } from '../fixtures/auth';

test.describe('Authentication Journey', () => {
  test.skip('should display login page for unauthenticated users', async ({ page }) => {
    // Placeholder — implement when Identity frontend is complete
    await page.goto('/');
    await expect(page).toHaveURL(/login/);
  });

  test.skip('should redirect to dashboard after login', async ({ authenticatedPage }) => {
    // Placeholder — implement when Identity frontend is complete
    await authenticatedPage.goto('/');
    await expect(authenticatedPage).toHaveURL(/dashboard/);
  });
});
