import { useCallback, useEffect, useState } from 'react';
import { Portal } from './Portal';

interface TourStep {
  target: string;
  title: string;
  description: string;
  position: 'top' | 'bottom' | 'left' | 'right';
}

const TOUR_STEPS: TourStep[] = [
  {
    target: '[data-tour="dashboard"]',
    title: 'Your Dashboard',
    description: 'This is your home base. See active sessions, match scores, and quick stats at a glance.',
    position: 'bottom',
  },
  {
    target: '[data-tour="browse"]',
    title: 'Browse Mentors',
    description: 'Find mentors across all Australian chapters. Each card shows your compatibility score.',
    position: 'bottom',
  },
  {
    target: '[data-tour="session-plan"]',
    title: 'Session Plans',
    description: 'AI-generated 35-minute session agendas with pre-work and follow-up tasks.',
    position: 'bottom',
  },
  {
    target: '[data-tour="notifications"]',
    title: 'Notifications',
    description: 'Get real-time updates when mentors accept requests, sessions are ready, or new opportunities appear.',
    position: 'bottom',
  },
  {
    target: '[data-tour="help"]',
    title: 'AI Help Assistant',
    description: 'Need help? Click the chat bubble anytime for instant answers about platform features.',
    position: 'left',
  },
];

const TOUR_STORAGE_KEY = 'gm_tour_completed';

/**
 * OnboardingTour — step-by-step overlay walkthrough for first-time users.
 * Steps: Dashboard → Browse → Session Plan → Notifications → Help
 * Keyboard navigable (Tab to advance, Escape to dismiss).
 * Stores completion in localStorage — doesn't show again.
 * Requirements: 25.1, 25.2, 25.3
 */
export function OnboardingTour() {
  const [isActive, setIsActive] = useState(false);
  const [currentStep, setCurrentStep] = useState(0);
  const [highlightRect, setHighlightRect] = useState<DOMRect | null>(null);

  // Check if tour should show (after onboarding complete, first-time only)
  useEffect(() => {
    const tourCompleted = localStorage.getItem(TOUR_STORAGE_KEY);
    if (tourCompleted) return;

    // Check if user just completed onboarding (flag set by OnboardingWizard)
    const justOnboarded = sessionStorage.getItem('gm_just_onboarded');
    if (justOnboarded) {
      sessionStorage.removeItem('gm_just_onboarded');
      // Small delay to allow dashboard to render
      setTimeout(() => setIsActive(true), 1000);
    }
  }, []);

  // Update highlight position based on current step's target element
  useEffect(() => {
    if (!isActive) return;

    const step = TOUR_STEPS[currentStep];
    if (!step) return;

    const targetEl = document.querySelector(step.target);
    if (targetEl) {
      const rect = targetEl.getBoundingClientRect();
      setHighlightRect(rect);
    } else {
      // If target doesn't exist, use center of screen
      setHighlightRect(null);
    }
  }, [isActive, currentStep]);

  // Keyboard navigation
  useEffect(() => {
    if (!isActive) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        dismiss();
      } else if (e.key === 'Tab' || e.key === 'Enter' || e.key === 'ArrowRight') {
        e.preventDefault();
        advance();
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        goBack();
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isActive, currentStep]);

  const dismiss = useCallback(() => {
    setIsActive(false);
    localStorage.setItem(TOUR_STORAGE_KEY, 'true');
  }, []);

  const advance = useCallback(() => {
    if (currentStep < TOUR_STEPS.length - 1) {
      setCurrentStep((prev) => prev + 1);
    } else {
      dismiss();
    }
  }, [currentStep, dismiss]);

  const goBack = useCallback(() => {
    if (currentStep > 0) {
      setCurrentStep((prev) => prev - 1);
    }
  }, [currentStep]);

  if (!isActive) return null;

  const step = TOUR_STEPS[currentStep]!;

  // Calculate tooltip position
  const tooltipStyle: React.CSSProperties = {};
  if (highlightRect) {
    switch (step.position) {
      case 'bottom':
        tooltipStyle.top = highlightRect.bottom + 16;
        tooltipStyle.left = Math.max(16, highlightRect.left + highlightRect.width / 2 - 160);
        break;
      case 'top':
        tooltipStyle.bottom = window.innerHeight - highlightRect.top + 16;
        tooltipStyle.left = Math.max(16, highlightRect.left + highlightRect.width / 2 - 160);
        break;
      case 'left':
        tooltipStyle.top = highlightRect.top + highlightRect.height / 2 - 60;
        tooltipStyle.right = window.innerWidth - highlightRect.left + 16;
        break;
      case 'right':
        tooltipStyle.top = highlightRect.top + highlightRect.height / 2 - 60;
        tooltipStyle.left = highlightRect.right + 16;
        break;
    }
  } else {
    // Center the tooltip if no target element found
    tooltipStyle.top = '50%';
    tooltipStyle.left = '50%';
    tooltipStyle.transform = 'translate(-50%, -50%)';
  }

  return (
    <Portal containerId="tour-root">
      {/* Dark overlay */}
      <div className="fixed inset-0 z-[1200] bg-black/70" aria-hidden="true" />

      {/* Highlight cut-out */}
      {highlightRect && (
        <div
          className="fixed z-[1201] border-2 border-violet rounded-xl pointer-events-none glow-violet"
          style={{
            top: highlightRect.top - 8,
            left: highlightRect.left - 8,
            width: highlightRect.width + 16,
            height: highlightRect.height + 16,
          }}
          aria-hidden="true"
        />
      )}

      {/* Tooltip card */}
      <div
        role="dialog"
        aria-modal="true"
        aria-label={`Tour step ${currentStep + 1} of ${TOUR_STEPS.length}: ${step.title}`}
        aria-live="polite"
        className="fixed z-[1202] w-[320px] glass-card p-5"
        style={tooltipStyle}
      >
        {/* Step counter */}
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs text-text-muted">
            Step {currentStep + 1} of {TOUR_STEPS.length}
          </span>
          <button
            onClick={dismiss}
            className="text-xs text-text-muted hover:text-text-primary transition-colors"
            aria-label="Skip tour"
          >
            Skip
          </button>
        </div>

        <h3 className="text-sm font-bold text-text-primary mb-1" style={{ fontFamily: 'Outfit, sans-serif' }}>
          {step.title}
        </h3>
        <p className="text-xs text-text-secondary mb-4">{step.description}</p>

        {/* Progress dots */}
        <div className="flex items-center gap-1.5 mb-4">
          {TOUR_STEPS.map((_, i) => (
            <div
              key={i}
              className={`w-2 h-2 rounded-full transition-colors ${
                i === currentStep ? 'bg-violet' : i < currentStep ? 'bg-violet/40' : 'bg-white/10'
              }`}
              aria-hidden="true"
            />
          ))}
        </div>

        {/* Navigation buttons */}
        <div className="flex items-center justify-between">
          {currentStep > 0 ? (
            <button
              onClick={goBack}
              className="text-xs text-text-muted hover:text-text-primary transition-colors"
              aria-label="Previous step"
            >
              ← Back
            </button>
          ) : (
            <div />
          )}
          <button
            onClick={advance}
            className="btn-violet text-xs px-4 py-2"
            aria-label={currentStep < TOUR_STEPS.length - 1 ? 'Next step' : 'Finish tour'}
          >
            {currentStep < TOUR_STEPS.length - 1 ? 'Next' : 'Got it!'}
          </button>
        </div>

        <p className="text-xs text-text-muted mt-3 text-center">
          Press Tab to advance, Escape to dismiss
        </p>
      </div>
    </Portal>
  );
}
