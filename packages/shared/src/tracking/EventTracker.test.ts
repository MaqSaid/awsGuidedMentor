import { describe, it, expect, beforeEach } from 'vitest';
import { EventTracker } from './EventTracker';

/**
 * Unit tests for EventTracker — event buffering, consent gating, and role tagging.
 *
 * Validates: Requirements 30.1, 30.3, 30.7, 30.8, 30.11
 */
describe('EventTracker', () => {
  let tracker: EventTracker;

  beforeEach(() => {
    tracker = new EventTracker({ consent: 'granted', sessionId: 'test-session' });
  });

  describe('Event Buffering (Req 30.3)', () => {
    it('should buffer tracked events', () => {
      tracker.track('page_view', { pageName: 'browse' });
      tracker.track('click', { element: 'mentor-card' });

      expect(tracker.getBufferSize()).toBe(2);
    });

    it('should include all required event fields', () => {
      tracker.track('page_view', { pageName: 'dashboard' });

      const events = tracker.getBuffer();
      expect(events[0]).toMatchObject({
        eventType: 'page_view',
        eventData: { pageName: 'dashboard' },
        sessionId: 'test-session',
        activeRole: 'mentee',
      });
      expect(events[0]!.timestamp).toBeGreaterThan(0);
    });
  });

  describe('Active Role Tagging (Req 30.11)', () => {
    it('should tag events with mentee role by default', () => {
      tracker.track('page_view');

      const events = tracker.getBuffer();
      expect(events[0]!.activeRole).toBe('mentee');
    });

    it('should tag events with mentor role after setActiveRole', () => {
      tracker.setActiveRole('mentor');
      tracker.track('click', { element: 'accept-btn' });

      const events = tracker.getBuffer();
      expect(events[0]!.activeRole).toBe('mentor');
    });

    it('should change role mid-session', () => {
      tracker.track('page_view');
      tracker.setActiveRole('mentor');
      tracker.track('click');

      const events = tracker.getBuffer();
      expect(events[0]!.activeRole).toBe('mentee');
      expect(events[1]!.activeRole).toBe('mentor');
    });
  });

  describe('Consent Management (Req 30.7, 30.8)', () => {
    it('should track all events when consent is granted', () => {
      tracker.setConsent('granted');
      tracker.track('page_view');
      tracker.track('click');
      tracker.track('form_step');

      expect(tracker.getBufferSize()).toBe(3);
    });

    it('should block non-essential events when consent is denied', () => {
      tracker.setConsent('denied');
      tracker.track('page_view');
      tracker.track('click');
      tracker.track('form_step');

      expect(tracker.getBufferSize()).toBe(0);
    });

    it('should allow auth events when consent is denied', () => {
      tracker.setConsent('denied');
      tracker.track('auth', { action: 'login' });

      expect(tracker.getBufferSize()).toBe(1);
      expect(tracker.getBuffer()[0]!.eventType).toBe('auth');
    });

    it('should allow error_encountered events when consent is denied', () => {
      tracker.setConsent('denied');
      tracker.track('error_encountered', { errorType: 'api_500', page: '/browse' });

      expect(tracker.getBufferSize()).toBe(1);
      expect(tracker.getBuffer()[0]!.eventType).toBe('error_encountered');
    });

    it('should discard buffered non-essential events when consent changes to denied', () => {
      tracker.track('page_view');
      tracker.track('click');
      tracker.track('error_encountered', { errorType: 'timeout' });

      tracker.setConsent('denied');

      // Only error_encountered (essential) should remain
      expect(tracker.getBufferSize()).toBe(1);
      expect(tracker.getBuffer()[0]!.eventType).toBe('error_encountered');
    });

    it('should report consent status correctly', () => {
      expect(tracker.getConsent()).toBe('granted');
      tracker.setConsent('denied');
      expect(tracker.getConsent()).toBe('denied');
    });
  });

  describe('Event Types (Req 30.1)', () => {
    it('should track page_view events', () => {
      tracker.track('page_view', { pageName: 'browse' });
      expect(tracker.getBuffer()[0]!.eventType).toBe('page_view');
    });

    it('should track click events', () => {
      tracker.track('click', { element: 'mentor-card', mentorId: '123' });
      expect(tracker.getBuffer()[0]!.eventType).toBe('click');
    });

    it('should track form_step events', () => {
      tracker.track('form_step', { formName: 'onboarding', step: 2, duration: 45000 });
      expect(tracker.getBuffer()[0]!.eventType).toBe('form_step');
    });

    it('should track error_encountered events', () => {
      tracker.track('error_encountered', { errorType: 'api_timeout', page: '/sessions' });
      expect(tracker.getBuffer()[0]!.eventType).toBe('error_encountered');
    });

    it('should track accessibility_feature events', () => {
      tracker.track('accessibility_feature', { feature: 'keyboard_nav' });
      expect(tracker.getBuffer()[0]!.eventType).toBe('accessibility_feature');
    });
  });
});
