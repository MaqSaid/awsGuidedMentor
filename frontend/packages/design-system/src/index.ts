/**
 * @guided-mentor/design-system
 *
 * Shared design system package for GuidedMentor platform.
 * Includes CSS tokens, glassmorphism utilities, React components, and accessibility utilities.
 *
 * Usage:
 *   import '@guided-mentor/design-system/tokens.css';
 *   import '@guided-mentor/design-system/glass.css';
 *   import { Button, Input, Modal } from '@guided-mentor/design-system/components';
 *   import { SkipNavLink, useFocusTrap, AriaLiveAnnouncer, useAnnounce } from '@guided-mentor/design-system/a11y';
 *   import { handleApiError } from '@guided-mentor/design-system/utils';
 *   import { useDebounceValidation, useRetry } from '@guided-mentor/design-system/hooks';
 *   import type { ApiErrorResponse } from '@guided-mentor/design-system/types';
 */

// Re-export all components
export * from './components';

// Re-export accessibility utilities
export * from './a11y';

// Re-export types
export * from './types';

// Re-export utilities
export * from './utils';

// Re-export hooks
export * from './hooks';
