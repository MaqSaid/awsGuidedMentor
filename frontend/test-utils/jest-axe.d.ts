declare module 'jest-axe' {
  import { AxeResults, RunOptions, Spec } from 'axe-core';

  export interface AxeOptions extends RunOptions {
    rules?: Record<string, { enabled: boolean }>;
  }

  export function axe(
    container: Element | string,
    options?: AxeOptions
  ): Promise<AxeResults>;

  export function toHaveNoViolations(): {
    toHaveNoViolations(): { pass: boolean; message(): string };
  };
}
