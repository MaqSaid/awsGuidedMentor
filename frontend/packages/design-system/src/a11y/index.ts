/**
 * Accessibility utilities for GuidedMentor platform.
 *
 * Provides:
 * - SkipNavLink: keyboard bypass for repetitive navigation
 * - useFocusTrap: focus containment for modals and wizards
 * - AriaLiveAnnouncer + useAnnounce: dynamic content announcements for screen readers
 */

export { SkipNavLink, type SkipNavLinkProps } from './SkipNavLink';
export { useFocusTrap, type UseFocusTrapOptions } from './useFocusTrap';
export { AriaLiveAnnouncer, useAnnounce, type AriaLiveAnnouncerProps } from './AriaLiveAnnouncer';
