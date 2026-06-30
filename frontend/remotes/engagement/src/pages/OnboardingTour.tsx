/**
 * OnboardingTour — Step-by-step overlay with tooltips, keyboard nav
 * (Tab advance, Escape dismiss), aria-live announcements, dismissible,
 * restart from settings.
 *
 * Requirements: 25.1, 25.2, 25.3
 */
import { useState, useEffect, useCallback, useRef } from 'react';
import { useTourStatus, useDismissTour } from '../hooks/useApi';
import type { TourStep } from '../types';

const DEFAULT_TOUR_STEPS: TourStep[] = [
  {
    id: 'dashboard',
    target: '[data-tour="dashboard"]',
    title: 'Your Dashboard',
    content: 'This is your personalised dashboard. Here you can see active sessions, recommendations, and progress at a glance.',
    placement: 'bottom',
  },
  {
    id: 'browse',
    target: '[data-tour="browse"]',
    title: 'Browse Mentors',
    content: 'Find mentors from all Australian chapters ranked by compatibility. Click any mentor card to start the matching process.',
    placement: 'bottom',
  },
  {
    id: 'notifications',
    target: '[data-tour="notifications"]',
    title: 'Notifications',
    content: 'Stay updated with real-time notifications for requests, session plans, and reminders.',
    placement: 'left',
  },
  {
    id: 'ai-help',
    target: '[data-tour="ai-help"]',
    title: 'AI Help Assistant',
    content: 'Need help? Click the chat bubble or press Ctrl+H to ask questions about the platform anytime.',
    placement: 'left',
  },
  {
    id: 'role-toggle',
    target: '[data-tour="role-toggle"]',
    title: 'Role Toggle',
    content: 'Switch between mentor and mentee roles at any time. Each role has its own profile and dashboard.',
    placement: 'bottom',
  },
];

interface OnboardingTourProps {
  steps?: TourStep[];
  /** Called when tour is dismissed or completed */
  onComplete?: () => void;
  /** Force show the tour (e.g., from Settings restart) */
  forceShow?: boolean;
}

export function OnboardingTour({ steps = DEFAULT_TOUR_STEPS, onComplete, forceShow = false }: OnboardingTourProps) {
  const { data: tourStatus } = useTourStatus();
  const dismissTour = useDismissTour();
  const [currentStep, setCurrentStep] = useState(0);
  const [isVisible, setIsVisible] = useState(false);
  const [tooltipPosition, setTooltipPosition] = useState({ top: 0, left: 0 });
  const tooltipRef = useRef<HTMLDivElement>(null);
  const announcerRef = useRef<HTMLDivElement>(null);

  // Show tour only if not previously dismissed
  useEffect(() => {
    if (forceShow) {
      setIsVisible(true);
      setCurrentStep(0);
    } else if (tourStatus && !tourStatus.dismissed) {
      setIsVisible(true);
    }
  }, [tourStatus, forceShow]);

  // Position tooltip relative to target element
  useEffect(() => {
    if (!isVisible || currentStep >= steps.length) return;

    const step = steps[currentStep];
    if (!step) return;
    const target = document.querySelector(step.target);
    if (!target) {
      // If target not found, still show tooltip centered
      setTooltipPosition({ top: window.innerHeight / 2 - 100, left: window.innerWidth / 2 - 160 });
      return;
    }

    const rect = target.getBoundingClientRect();
    const pos = calculatePosition(rect, step.placement);
    setTooltipPosition(pos);

    // Highlight target
    target.classList.add('tour-highlight');
    return () => {
      target.classList.remove('tour-highlight');
    };
  }, [isVisible, currentStep, steps]);

  // Announce step change for screen readers
  useEffect(() => {
    if (isVisible && announcerRef.current && currentStep < steps.length) {
      const step = steps[currentStep];
      if (step) {
        announcerRef.current.textContent = `Tour step ${currentStep + 1} of ${steps.length}: ${step.title}. ${step.content}`;
      }
    }
  }, [isVisible, currentStep, steps]);

  // Keyboard navigation
  useEffect(() => {
    if (!isVisible) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.preventDefault();
        handleDismiss();
      } else if (e.key === 'Tab') {
        e.preventDefault();
        handleNext();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isVisible, currentStep]);

  const handleNext = useCallback(() => {
    if (currentStep < steps.length - 1) {
      setCurrentStep((prev) => prev + 1);
    } else {
      handleDismiss();
    }
  }, [currentStep, steps.length]);

  const handlePrev = useCallback(() => {
    if (currentStep > 0) {
      setCurrentStep((prev) => prev - 1);
    }
  }, [currentStep]);

  const handleDismiss = useCallback(() => {
    setIsVisible(false);
    dismissTour.mutate();
    onComplete?.();
  }, [dismissTour, onComplete]);

  if (!isVisible || currentStep >= steps.length) return null;

  const step = steps[currentStep];
  if (!step) return null;
  const isLastStep = currentStep === steps.length - 1;
  const isFirstStep = currentStep === 0;

  return (
    <>
      {/* Screen reader announcer */}
      <div ref={announcerRef} aria-live="assertive" aria-atomic="true" className="sr-only" />

      {/* Overlay backdrop */}
      <div
        className="fixed inset-0 z-[9998] bg-[rgba(0,0,0,0.5)]"
        aria-hidden="true"
      />

      {/* Tooltip */}
      <div
        ref={tooltipRef}
        role="dialog"
        aria-modal="true"
        aria-label={`Tour step ${currentStep + 1} of ${steps.length}`}
        className="fixed z-[9999] w-80 glass-card p-5 rounded-lg shadow-2xl"
        style={{ top: tooltipPosition.top, left: tooltipPosition.left }}
      >
        {/* Step indicator */}
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs text-text-muted">
            Step {currentStep + 1} of {steps.length}
          </span>
          <button
            onClick={handleDismiss}
            aria-label="Dismiss tour"
            className="text-text-muted hover:text-text-primary transition-colors p-1 rounded-sm focus-visible:ring-2 focus-visible:ring-primary outline-none"
          >
            <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <h3 className="text-base font-semibold text-text-primary mb-2">{step.title}</h3>
        <p className="text-sm text-text-secondary leading-relaxed">{step.content}</p>

        {/* Navigation buttons */}
        <div className="flex items-center justify-between mt-4">
          <button
            onClick={handlePrev}
            disabled={isFirstStep}
            aria-label="Previous step"
            className="text-sm text-text-secondary hover:text-text-primary disabled:opacity-30 disabled:cursor-not-allowed transition-colors px-2 py-1 rounded-sm focus-visible:ring-2 focus-visible:ring-primary outline-none"
          >
            ← Back
          </button>
          <div className="flex gap-1" aria-hidden="true">
            {steps.map((_, i) => (
              <span
                key={i}
                className={`h-1.5 w-1.5 rounded-full ${i === currentStep ? 'bg-primary' : 'bg-[rgba(255,255,255,0.2)]'}`}
              />
            ))}
          </div>
          <button
            onClick={handleNext}
            aria-label={isLastStep ? 'Finish tour' : 'Next step'}
            className="text-sm font-medium text-primary hover:text-primary/80 transition-colors px-2 py-1 rounded-sm focus-visible:ring-2 focus-visible:ring-primary outline-none"
          >
            {isLastStep ? 'Finish' : 'Next →'}
          </button>
        </div>

        {/* Keyboard hint */}
        <p className="text-xs text-text-muted mt-3 text-center">
          Tab to advance · Escape to dismiss
        </p>
      </div>
    </>
  );
}

function calculatePosition(
  rect: DOMRect,
  placement: 'top' | 'bottom' | 'left' | 'right'
): { top: number; left: number } {
  const tooltipWidth = 320;
  const tooltipHeight = 220;
  const gap = 12;

  switch (placement) {
    case 'bottom':
      return {
        top: rect.bottom + gap,
        left: Math.max(16, rect.left + rect.width / 2 - tooltipWidth / 2),
      };
    case 'top':
      return {
        top: rect.top - tooltipHeight - gap,
        left: Math.max(16, rect.left + rect.width / 2 - tooltipWidth / 2),
      };
    case 'left':
      return {
        top: rect.top + rect.height / 2 - tooltipHeight / 2,
        left: rect.left - tooltipWidth - gap,
      };
    case 'right':
      return {
        top: rect.top + rect.height / 2 - tooltipHeight / 2,
        left: rect.right + gap,
      };
    default:
      return { top: rect.bottom + gap, left: rect.left };
  }
}

export default OnboardingTour;
