import fc from 'fast-check';
import type { SessionPlan, AgendaItem, SessionRecord, SessionStatus } from '@guided-mentor/shared-types';
import { arbUuid, arbIsoTimestamp, arbAlphanumericString } from './common';

/**
 * Generates a valid agenda item with minimum 3-minute duration and max 500-char description.
 */
export const arbAgendaItem = (maxDuration = 35): fc.Arbitrary<AgendaItem> =>
  fc.record({
    title: fc.constantFrom(
      'Introduction and Goal Setting',
      'Technical Deep Dive',
      'Hands-on Exercise',
      'Architecture Review',
      'Code Review',
      'Q&A Session',
      'Action Items Discussion',
      'Progress Review',
      'Certification Prep',
      'Career Discussion',
    ),
    durationMinutes: fc.integer({ min: 3, max: Math.min(maxDuration, 20) }),
    description: arbAlphanumericString(10, 500),
  });

/**
 * Generates a valid list of agenda items whose durations sum to exactly 35 minutes.
 *
 * Constraints:
 * - 3-7 items
 * - Each item >= 3 minutes
 * - Total duration = 35 minutes
 */
export const arbAgendaItems = (): fc.Arbitrary<AgendaItem[]> =>
  fc.integer({ min: 3, max: 7 }).chain(numItems => {
    // Generate numItems durations that sum to 35, each >= 3
    return arbDurationPartition(35, numItems, 3).chain(durations =>
      fc.tuple(
        ...durations.map(dur =>
          fc.record({
            title: fc.constantFrom(
              'Introduction and Goal Setting',
              'Technical Deep Dive',
              'Hands-on Exercise',
              'Architecture Review',
              'Code Review',
              'Q&A Session',
              'Action Items Discussion',
            ),
            durationMinutes: fc.constant(dur),
            description: arbAlphanumericString(10, 500),
          })
        )
      ) as fc.Arbitrary<AgendaItem[]>
    );
  });

/**
 * Helper: generates an array of integers that sum to `total`,
 * each >= `min`, with exactly `count` elements.
 */
function arbDurationPartition(total: number, count: number, min: number): fc.Arbitrary<number[]> {
  // Remaining budget after giving each item the minimum
  const remaining = total - count * min;
  if (remaining < 0) {
    // Impossible partition — return minimum valid
    return fc.constant(Array(count).fill(min));
  }

  return fc.array(fc.integer({ min: 0, max: remaining }), { minLength: count, maxLength: count })
    .map(extras => {
      // Normalize extras to sum to `remaining`
      const sum = extras.reduce((a, b) => a + b, 0);
      if (sum === 0) {
        // Distribute evenly
        const base = Math.floor(remaining / count);
        const result = extras.map(() => min + base);
        // Add remainder to first item
        result[0] += remaining - base * count;
        return result;
      }
      // Scale proportionally
      const scaled = extras.map(e => Math.floor((e / sum) * remaining));
      const scaledSum = scaled.reduce((a, b) => a + b, 0);
      // Add remainder to first item to ensure exact sum
      scaled[0] += remaining - scaledSum;
      return scaled.map(e => e + min);
    });
}

/**
 * Generates a valid session plan meeting all schema constraints.
 *
 * Constraints:
 * - sessionTitle: max 100 chars
 * - agenda: 3-7 items, durations sum to exactly 35 min, each item >= 3 min
 * - preworkTasks: 2-5 items, each max 200 chars
 * - followUpTasks: 2-5 items, each max 200 chars
 */
export const arbSessionPlan = (): fc.Arbitrary<SessionPlan> =>
  fc.record({
    sessionTitle: arbAlphanumericString(5, 100),
    agenda: arbAgendaItems(),
    preworkTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
    followUpTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
  });

/**
 * Generates an invalid session plan (for negative testing).
 * One of the constraints will be violated.
 */
export const arbInvalidSessionPlan = (): fc.Arbitrary<{ plan: SessionPlan; reason: string }> =>
  fc.oneof(
    // Agenda durations don't sum to 35
    fc.record({
      sessionTitle: arbAlphanumericString(5, 100),
      agenda: fc.array(arbAgendaItem(15), { minLength: 3, maxLength: 7 }),
      preworkTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
      followUpTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
    }).filter(plan => {
      const sum = plan.agenda.reduce((s, item) => s + item.durationMinutes, 0);
      return sum !== 35;
    }).map(plan => ({ plan, reason: 'agenda_duration_not_35' })),

    // Too few agenda items (< 3)
    fc.record({
      sessionTitle: arbAlphanumericString(5, 100),
      agenda: fc.array(arbAgendaItem(30), { minLength: 1, maxLength: 2 }),
      preworkTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
      followUpTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
    }).map(plan => ({ plan, reason: 'too_few_agenda_items' })),

    // Too few prework tasks (< 2)
    fc.record({
      sessionTitle: arbAlphanumericString(5, 100),
      agenda: arbAgendaItems(),
      preworkTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 0, maxLength: 1 }),
      followUpTasks: fc.array(arbAlphanumericString(5, 200), { minLength: 2, maxLength: 5 }),
    }).map(plan => ({ plan, reason: 'too_few_prework_tasks' })),
  );

/** Generates a valid session status */
export const arbSessionStatus = (): fc.Arbitrary<SessionStatus> =>
  fc.constantFrom<SessionStatus>(
    'pending', 'active', 'completed', 'unresolved', 'pending_plan', 'plan_failed'
  );

/**
 * Generates a complete valid SessionRecord.
 */
export const arbSessionRecord = (): fc.Arbitrary<SessionRecord> =>
  fc.record({
    sessionId: arbUuid(),
    menteeId: arbUuid(),
    mentorId: arbUuid(),
    status: arbSessionStatus(),
    sessionPlan: fc.option(arbSessionPlan(), { nil: null }),
    menteeCompletedAt: fc.option(arbIsoTimestamp(), { nil: null }),
    mentorCompletedAt: fc.option(arbIsoTimestamp(), { nil: null }),
    checklistState: arbSessionPlan().map(plan => ({
      prework: plan.preworkTasks.map(() => false),
      followup: plan.followUpTasks.map(() => false),
    })),
    lockId: arbUuid(),
    lockExpiresAt: arbIsoTimestamp(),
    planRetryCount: fc.integer({ min: 0, max: 3 }),
    createdAt: arbIsoTimestamp(),
    updatedAt: arbIsoTimestamp(),
  });
