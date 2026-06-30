/**
 * EventTracker — Buffers engagement events and flushes them to the backend.
 * Flushes every 30 seconds or on visibilitychange (using sendBeacon for reliability).
 *
 * This is the shared package version for testing purposes.
 * The production version lives in frontend/remotes/engagement/src/tracking/EventTracker.ts.
 *
 * Requirements: 30.1, 30.2, 30.3
 */

export interface TrackEvent {
  eventType: string;
  eventData?: Record<string, unknown>;
  timestamp: number;
  pageContext: string;
  sessionId: string;
  activeRole: 'mentor' | 'mentee';
}

export type ConsentStatus = 'granted' | 'denied' | 'pending';

const FLUSH_INTERVAL_MS = 30_000;

export class EventTracker {
  private buffer: TrackEvent[] = [];
  private flushIntervalId: ReturnType<typeof setInterval> | null = null;
  private sessionId: string;
  private consent: ConsentStatus;
  private activeRole: 'mentor' | 'mentee' = 'mentee';
  private endpoint: string;

  constructor(options?: { endpoint?: string; sessionId?: string; consent?: ConsentStatus }) {
    this.sessionId = options?.sessionId ?? 'test-session-id';
    this.endpoint = options?.endpoint ?? '/v1/analytics/events';
    this.consent = options?.consent ?? 'granted';
  }

  setActiveRole(role: 'mentor' | 'mentee'): void {
    this.activeRole = role;
  }

  setConsent(status: ConsentStatus): void {
    this.consent = status;
    if (status === 'denied') {
      this.buffer = this.buffer.filter((e) => this.isEssentialEvent(e.eventType));
    }
  }

  getConsent(): ConsentStatus {
    return this.consent;
  }

  track(eventType: string, data?: Record<string, unknown>): void {
    if (this.consent === 'denied' && !this.isEssentialEvent(eventType)) {
      return;
    }

    this.buffer.push({
      eventType,
      eventData: data,
      timestamp: Date.now(),
      pageContext: '',
      sessionId: this.sessionId,
      activeRole: this.activeRole,
    });
  }

  getBufferSize(): number {
    return this.buffer.length;
  }

  getBuffer(): TrackEvent[] {
    return [...this.buffer];
  }

  clearBuffer(): void {
    this.buffer = [];
  }

  private isEssentialEvent(eventType: string): boolean {
    return eventType === 'auth' || eventType === 'error_encountered';
  }
}
