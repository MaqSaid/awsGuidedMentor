import { type CSSProperties } from 'react';

const srOnlyStyles: CSSProperties = {
  position: 'absolute',
  width: '1px',
  height: '1px',
  padding: 0,
  margin: '-1px',
  overflow: 'hidden',
  clip: 'rect(0, 0, 0, 0)',
  whiteSpace: 'nowrap',
  borderWidth: 0,
};

const focusVisibleStyles: CSSProperties = {
  position: 'fixed',
  top: '0.5rem',
  left: '0.5rem',
  zIndex: 9999,
  width: 'auto',
  height: 'auto',
  padding: '0.75rem 1.5rem',
  margin: 0,
  overflow: 'visible',
  clip: 'auto',
  whiteSpace: 'normal',
  background: '#1a1a2e',
  color: '#ffffff',
  fontSize: '0.875rem',
  fontWeight: 600,
  borderRadius: '0.375rem',
  border: '2px solid #60a5fa',
  textDecoration: 'none',
  outline: '2px solid #60a5fa',
  outlineOffset: '2px',
};

export interface SkipNavLinkProps {
  /** Target anchor id (without #). Defaults to "main-content". */
  targetId?: string;
  /** Link text. Defaults to "Skip to main content". */
  children?: string;
}

/**
 * SkipNavLink — Visually hidden link that becomes visible on focus.
 * Must be the first focusable element in the DOM.
 * Allows keyboard users to bypass repetitive navigation.
 */
export function SkipNavLink({
  targetId = 'main-content',
  children = 'Skip to main content',
}: SkipNavLinkProps) {
  return (
    <a
      href={`#${targetId}`}
      style={srOnlyStyles}
      onFocus={(e) => {
        Object.assign(e.currentTarget.style, focusVisibleStyles);
      }}
      onBlur={(e) => {
        Object.assign(e.currentTarget.style, srOnlyStyles);
      }}
    >
      {children}
    </a>
  );
}
