import { axe, toHaveNoViolations } from 'jest-axe';
import { expect } from 'vitest';

// Extend Vitest expect with jest-axe matchers
expect.extend(toHaveNoViolations);

/**
 * Runs axe-core accessibility audit on a container element.
 * Asserts that no accessibility violations are found.
 * 
 * In CI this blocks deployments when accessibility score drops below 90%.
 * Score = (total_checks - violations) / total_checks * 100
 * 
 * @param container - The HTML element to audit
 * @param options - Optional axe-core configuration
 */
export async function expectAccessible(
  container: Element,
  options?: Parameters<typeof axe>[1]
): Promise<void> {
  const results = await axe(container, {
    ...options,
    rules: {
      // Ensure WCAG 2.1 AA compliance
      region: { enabled: true },
      ...options?.rules,
    },
  });

  // Calculate score: (passes / total) * 100
  const totalChecks = results.passes.length + results.violations.length;
  const score = totalChecks > 0
    ? Math.round((results.passes.length / totalChecks) * 100)
    : 100;

  // Block if score < 90
  if (score < 90) {
    const violationSummary = results.violations
      .map(v => `  - ${v.id}: ${v.description} (${v.impact}) [${v.nodes.length} instance(s)]`)
      .join('\n');
    
    throw new Error(
      `Accessibility score ${score}% is below the required 90% threshold.\n` +
      `Violations:\n${violationSummary}`
    );
  }

  expect(results).toHaveNoViolations();
}
