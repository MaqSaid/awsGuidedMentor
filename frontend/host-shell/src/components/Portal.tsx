import { createPortal } from 'react-dom';
import { useEffect, useState, type ReactNode } from 'react';

interface PortalProps {
  children: ReactNode;
  containerId?: string;
}

/**
 * Portal — renders children outside the parent DOM tree.
 * Creates a container div at document.body level to avoid z-index/overflow issues.
 */
export function Portal({ children, containerId = 'portal-root' }: PortalProps) {
  const [container, setContainer] = useState<HTMLElement | null>(null);

  useEffect(() => {
    let el = document.getElementById(containerId);
    if (!el) {
      el = document.createElement('div');
      el.id = containerId;
      el.setAttribute('aria-live', 'polite');
      document.body.appendChild(el);
    }
    setContainer(el);

    return () => {
      // Only remove if we created it and it's empty
      if (el && el.childNodes.length === 0) {
        el.remove();
      }
    };
  }, [containerId]);

  if (!container) return null;
  return createPortal(children, container);
}
