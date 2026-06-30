/**
 * EventTracker — Buffers engagement events and flushes them to the backend.
 * Flushes every 30 seconds or on visibilitychange (using sendBeacon for reliability).
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

const CONSENT_STORAGE_KEY = 'gm_tracking_consent';
const FLUSH_INTERVAL_MS = 30_000;

export class EventTracker {
  private buffer: TrackEvent[] = [];
  private flushIntervalId: ReturnType<typeof setInterval> | null = null;
  private sessionId: string;
  private consent: ConsentStatus;
  private activeRole: 'mentor' | 'mentee' = 'mentee';
  private visibilityHandler: (() => void) | null = null;
  private endpoint: string;

  constructor(endpoint = '/v1/analytics/events') {
    this.sessionId = crypto.randomUUID();
    this.endpoint = endpoint;
    this.consent = this.loadConsent();
    this.startFlushInterval();
    this.registerVisibilityHandler();
  }

  /** Update the active role tag for subsequent events. */
  setActiveRole(role: 'mentor' | 'mentee'): void {
    this.activeRole = role;
  }

  /** Update consent status. When denied, buffer is cleared and only auth/error events are tracked. */
  setConsent(status: ConsentStatus): void {
    this.consent = status;
    localStorage.setItem(CONSENT_STORAGE_KEY, status);

    if (status === 'denied') {
      // Discard non-essential buffered events
      this.buffer = this.buffer.filter((e) => this.isEssentialEvent(e.eventType));
    }
  }

  getConsent(): ConsentStatus {
    return this.consent;
  }

  /** Track an event. Respects consent: when denied, only auth/error events are accepted. */
  track(eventType: string, data?: Record<string, unknown>): void {
    if (this.consent === 'denied' && !this.isEssentialEvent(eventType)) {
      return;
    }

    this.buffer.push({
      eventType,
      eventData: data,
      timestamp: Date.now(),
      pageContext: typeof window !== 'undefined' ? window.location.pathname : '',
      sessionId: this.sessionId,
      activeRole: this.activeRole,
    });
  }

  /** Flush buffered events to the backend via fetch. Re-queues on failure. */
  async flush(): Promise<void> {
    if (this.buffer.length === 0) return;

    const events = [...this.buffer];
    this.buffer = [];

    try {
      const response = await fetch(this.endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ events }),
      });

      if (!response.ok) {
        // Re-add events to buffer for next flush attempt
        this.buffer.unshift(...events);
      }
    } catch {
      // On network failure, re-add events to buffer
      this.buffer.unshift(...events);
    }
  }

  /** Flush via sendBeacon (reliable on page hide). Returns true if beacon was queued. */
  flushBeacon(): boolean {
    if (this.buffer.length === 0) return true;

    const events = [...this.buffer];
    const payload = JSON.stringify({ events });
    const queued = navigator.sendBeacon(this.endpoint, payload);

    if (queued) {
      this.buffer = [];
    }

    return queued;
  }

  /** Clean up intervals and event listeners. */
  destroy(): void {
    if (this.flushIntervalId !== null) {
      clearInterval(this.flushIntervalId);
      this.flushIntervalId = null;
    }

    if (this.visibilityHandler && typeof document !== 'undefined') {
      document.removeEventListener('visibilitychange', this.visibilityHandler);
      this.visibilityHandler = null;
    }
  }

  /** Get the current buffer length (useful for testing). */
  getBufferSize(): number {
    return this.buffer.length;
  }

  private isEssentialEvent(eventType: string): boolean {
    return eventType === 'auth' || eventType === 'error_encountered';
  }

  private loadConsent(): ConsentStatus {
    if (typeof localStorage === 'undefined') return 'pending';
    const stored = localStorage.getItem(CONSENT_STORAGE_KEY);
    if (stored === 'granted' || stored === 'denied') return stored;
    return 'pending';
  }

  private startFlushInterval(): void {
    this.flushIntervalId = setInterval(() => {
      void this.flush();
    }, FLUSH_INTERVAL_MS);
  }

  private registerVisibilityHandler(): void {
    if (typeof document === 'undefined') return;

    this.visibilityHandler = () => {
      if (document.visibilityState === 'hidden') {
        this.flushBeacon();
      }
    };

    document.addEventListener('visibilitychange', this.visibilityHandler);
  }
}
