import { useEffect, useRef, useCallback, type RefObject } from 'react';

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(', ');

export interface UseFocusTrapOptions {
  /** Whether the focus trap is currently active. Defaults to true. */
  enabled?: boolean;
  /** Callback fired when Escape key is pressed. If not provided, Escape does nothing. */
  onEscape?: () => void;
  /** Whether to return focus to the previously focused element on cleanup. Defaults to true. */
  returnFocusOnDeactivate?: boolean;
}

/**
 * useFocusTrap — Custom hook that traps Tab/Shift+Tab focus within a container.
 * Used by Modal, ConfirmDialog, and OnboardingWizard.
 *
 * @returns A ref to attach to the container element.
 */
export function useFocusTrap<T extends HTMLElement = HTMLDivElement>(
  options: UseFocusTrapOptions = {}
): RefObject<T | null> {
  const { enabled = true, onEscape, returnFocusOnDeactivate = true } = options;
  const containerRef = useRef<T | null>(null);
  const previousFocusRef = useRef<Element | null>(null);

  const getFocusableElements = useCallback((): HTMLElement[] => {
    if (!containerRef.current) return [];
    const elements = containerRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR);
    return Array.from(elements).filter(
      (el) => !el.hasAttribute('disabled') && el.offsetParent !== null
    );
  }, []);

  useEffect(() => {
    if (!enabled) return;

    // Store the currently focused element to restore later
    previousFocusRef.current = document.activeElement;

    // Focus the first focusable element within the trap
    const focusableElements = getFocusableElements();
    if (focusableElements.length > 0) {
      focusableElements[0]?.focus();
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && onEscape) {
        event.preventDefault();
        onEscape();
        return;
      }

      if (event.key !== 'Tab') return;

      const focusable = getFocusableElements();
      if (focusable.length === 0) return;

      const firstElement = focusable[0]!;
      const lastElement = focusable[focusable.length - 1]!;

      if (event.shiftKey) {
        // Shift+Tab: if focused on first, wrap to last
        if (document.activeElement === firstElement) {
          event.preventDefault();
          lastElement.focus();
        }
      } else {
        // Tab: if focused on last, wrap to first
        if (document.activeElement === lastElement) {
          event.preventDefault();
          firstElement.focus();
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);

      // Return focus to the previously focused element
      if (returnFocusOnDeactivate && previousFocusRef.current instanceof HTMLElement) {
        previousFocusRef.current.focus();
      }
    };
  }, [enabled, onEscape, returnFocusOnDeactivate, getFocusableElements]);

  return containerRef;
}
