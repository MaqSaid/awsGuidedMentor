import { useOptimistic, useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ScoreRing } from '../components/ScoreRing';
import { SessionPlanSkeleton } from '../components/Skeleton';
import { apiFetch } from '../lib/api';

interface AgendaItem {
  timeRange: string;
  type: 'intro' | 'discussion' | 'exercise' | 'planning' | 'wrap-up';
  title: string;
  description: string;
}

interface FollowUpTask {
  title: string;
  priority: 'high' | 'medium' | 'low';
}

interface BookedSession {
  sessionId: string;
  mentor: { displayName: string; initials: string; title: string; company: string };
  date: string;
  pending?: boolean;
}

interface PlanData {
  sessionId: string;
  mentee: { displayName: string; initials: string; goal: string; skills: string[] };
  mentor: { displayName: string; initials: string; title: string; company: string };
  compatibilityScore: number;
  matchDescription: string;
  keyStrengths: string[];
  agenda: AgendaItem[];
  followUpTasks: FollowUpTask[];
  status?: 'active' | 'completed';
  bookedSessions?: BookedSession[];
  pendingAction?: OptimisticAction;
}

type OptimisticAction =
  | { type: 'complete'; sessionId: string }
  | { type: 'book'; sessionId: string; mentor: { displayName: string; initials: string; title: string; company: string }; date: string };

const typeBadgeStyles: Record<string, string> = {
  intro: 'bg-blue/20 text-blue',
  discussion: 'bg-violet/20 text-violet-light',
  exercise: 'bg-rose/20 text-rose',
  planning: 'bg-amber/20 text-amber',
  'wrap-up': 'bg-white/5 text-text-secondary',
};

const priorityStyles: Record<string, string> = {
  high: 'bg-rose/20 text-rose',
  medium: 'bg-amber/20 text-amber',
  low: 'bg-mint/20 text-mint',
};

export default function SessionPlan() {
  const { id } = useParams<{ id: string }>();
  const sessionId = id ?? 'session-001';
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  // Clear error toast after 5 seconds
  useEffect(() => {
    if (!error) return;
    const timer = setTimeout(() => setError(null), 5000);
    return () => clearTimeout(timer);
  }, [error]);

  // Fetch session plan data via TanStack Query
  const { data: serverData, isLoading } = useQuery<PlanData>({
    queryKey: ['session-plan', sessionId],
    queryFn: async () => {
      const response = await apiFetch(`/v1/sessions/${sessionId}/plan`);
      if (!response.ok) throw new Error('Failed to load session plan');
      return response.json() as Promise<PlanData>;
    },
  });

  // Optimistic state for session completion and booking
  const [optimisticData, addOptimistic] = useOptimistic(
    serverData ?? null,
    (current: PlanData | null, action: OptimisticAction) => {
      if (!current) return current;
      if (action.type === 'complete') {
        return {
          ...current,
          status: 'completed' as const,
          pendingAction: action,
        };
      }
      if (action.type === 'book') {
        const newSession: BookedSession = {
          sessionId: action.sessionId,
          mentor: action.mentor,
          date: action.date,
          pending: true,
        };
        return {
          ...current,
          bookedSessions: [...(current.bookedSessions ?? []), newSession],
          pendingAction: action,
        };
      }
      return current;
    }
  );

  // Mutation for marking session complete with 10s timeout
  const completeMutation = useMutation({
    mutationFn: async (targetSessionId: string) => {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 10_000);

      try {
        const response = await apiFetch(`/v1/sessions/${targetSessionId}/complete`, {
          method: 'POST',
          signal: controller.signal,
        });
        if (!response.ok) throw new Error('Server rejected the request');
        return response.json();
      } finally {
        clearTimeout(timeoutId);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session-plan', sessionId] });
    },
    onError: (err: Error) => {
      const message = err.name === 'AbortError'
        ? 'Request timed out. Please try again.'
        : 'Failed to mark session complete. Please try again.';
      setError(message);
    },
  });

  const handleMarkComplete = () => {
    addOptimistic({ type: 'complete', sessionId });
    completeMutation.mutate(sessionId);
  };

  // Mutation for booking a session with 10s timeout
  const bookMutation = useMutation({
    mutationFn: async (bookingData: { sessionId: string; mentorId: string; date: string }) => {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 10_000);

      try {
        const response = await apiFetch(`/v1/sessions/book`, {
          method: 'POST',
          body: JSON.stringify(bookingData),
          signal: controller.signal,
        });
        if (!response.ok) throw new Error('Server rejected the booking request');
        return response.json();
      } finally {
        clearTimeout(timeoutId);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session-plan', sessionId] });
    },
    onError: (err: Error) => {
      const message = err.name === 'AbortError'
        ? 'Booking timed out. Please try again.'
        : 'Failed to book session. Please try again.';
      setError(message);
    },
  });

  const handleBookSession = (mentorId: string, mentor: { displayName: string; initials: string; title: string; company: string }, date: string) => {
    const newSessionId = `session-${Date.now()}`;
    addOptimistic({ type: 'book', sessionId: newSessionId, mentor, date });
    bookMutation.mutate({ sessionId: newSessionId, mentorId, date });
  };

  if (isLoading || !optimisticData) {
    return <SessionPlanSkeleton />;
  }

  const isPending = completeMutation.isPending;
  const isBookingPending = bookMutation.isPending;
  const isCompleted = optimisticData.status === 'completed';
  const bookedSessions = optimisticData.bookedSessions ?? [];

  return (
    <section className="max-w-4xl mx-auto px-4 md:px-6 py-6 md:py-10">
      {/* Error toast — aria-live polite region */}
      {error && (
        <div
          role="alert"
          aria-live="polite"
          className="mb-4 p-3 rounded-lg bg-rose/20 text-rose text-sm text-center"
        >
          {error}
        </div>
      )}

      {/* Header: Mentee — Score — Mentor */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 sm:gap-6 mb-6">
        {/* Mentee card */}
        <div className="glass-card p-4 flex items-center gap-3 flex-1">
          <div className="w-12 h-12 rounded-full bg-mint/20 border-2 border-mint/40 flex items-center justify-center text-sm font-bold text-mint">
            {optimisticData.mentee.initials}
          </div>
          <div>
            <p className="font-semibold text-text-primary">{optimisticData.mentee.displayName}</p>
            <p className="text-xs text-text-secondary">{optimisticData.mentee.goal}</p>
          </div>
        </div>

        {/* Score ring (center) */}
        <div className="flex flex-col items-center">
          <ScoreRing score={optimisticData.compatibilityScore} size="lg" />
          <span className="mt-2 text-xs px-2 py-0.5 rounded-full bg-violet/20 text-violet-light font-medium">
            Generated by AI
          </span>
        </div>

        {/* Mentor card */}
        <div className="glass-card p-4 flex items-center gap-3 flex-1">
          <div className="w-12 h-12 rounded-full bg-violet/20 border-2 border-violet/40 flex items-center justify-center text-sm font-bold text-violet-light">
            {optimisticData.mentor.initials}
          </div>
          <div>
            <p className="font-semibold text-text-primary">{optimisticData.mentor.displayName}</p>
            <p className="text-xs text-text-secondary">{optimisticData.mentor.title} at {optimisticData.mentor.company}</p>
          </div>
        </div>
      </div>

      {/* Match description */}
      <p className="text-sm text-text-secondary italic text-center mb-4">
        {optimisticData.matchDescription}
      </p>

      {/* Key strengths */}
      <div className="flex items-center justify-center gap-2 mb-10 flex-wrap">
        <span className="text-sm text-text-secondary font-medium">Key strength:</span>
        {optimisticData.keyStrengths.map((skill) => (
          <span
            key={skill}
            className="text-xs px-2.5 py-1 rounded-full bg-violet/20 text-violet-light font-medium"
          >
            {skill}
          </span>
        ))}
      </div>

      {/* Agenda */}
      <section aria-label="Session agenda" className="mb-10">
        <h2 className="text-xl font-semibold mb-4" style={{ fontFamily: 'Outfit, sans-serif' }}>
          35-Minute Session Agenda
        </h2>
        <div className="space-y-3">
          {optimisticData.agenda.map((item, i) => (
            <div key={i} className="glass-card p-4 flex flex-col sm:flex-row gap-2 sm:gap-4">
              <div className="shrink-0 sm:w-24 text-sm text-text-muted font-mono">
                {item.timeRange}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${typeBadgeStyles[item.type] ?? ''}`}>
                    {item.type}
                  </span>
                  <h3 className="font-medium text-text-primary text-sm">{item.title}</h3>
                </div>
                <p className="text-sm text-text-secondary">{item.description}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Follow-up tasks */}
      <section aria-label="Follow-up tasks" className="mb-10">
        <h2 className="text-xl font-semibold mb-4" style={{ fontFamily: 'Outfit, sans-serif' }}>
          Follow-up Tasks
        </h2>
        <div className="space-y-2">
          {optimisticData.followUpTasks.map((task, i) => (
            <div key={i} className="glass-card p-4 flex items-center justify-between">
              <span className="text-sm text-text-primary">{task.title}</span>
              <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${priorityStyles[task.priority] ?? ''}`}>
                {task.priority}
              </span>
            </div>
          ))}
        </div>
      </section>

      {/* Booked Sessions */}
      {bookedSessions.length > 0 && (
        <section aria-label="Booked sessions" className="mb-10">
          <h2 className="text-xl font-semibold mb-4" style={{ fontFamily: 'Outfit, sans-serif' }}>
            Booked Sessions
          </h2>
          <div className="space-y-2">
            {bookedSessions.map((session) => (
              <div
                key={session.sessionId}
                className={`glass-card p-4 flex items-center justify-between ${session.pending ? 'opacity-50' : ''}`}
                aria-busy={session.pending ? true : undefined}
              >
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-violet/20 border-2 border-violet/40 flex items-center justify-center text-xs font-bold text-violet-light">
                    {session.mentor.initials}
                  </div>
                  <div>
                    <p className="text-sm font-medium text-text-primary">{session.mentor.displayName}</p>
                    <p className="text-xs text-text-secondary">{session.date}</p>
                  </div>
                </div>
                <span className="text-xs px-2 py-0.5 rounded-full bg-amber/20 text-amber font-medium">
                  {session.pending ? 'Confirming...' : 'Booked'}
                </span>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Book a Session button */}
      <div className="mb-4">
        <button
          className="btn-violet w-full text-base py-4"
          onClick={() => handleBookSession(
            'mentor-0001',
            optimisticData.mentor,
            new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toLocaleDateString()
          )}
          disabled={isBookingPending}
        >
          {isBookingPending ? 'Booking...' : 'Book a Session →'}
        </button>
      </div>

      {/* Complete button — shows pending state with opacity-50 + aria-busy */}
      <div
        className={isPending ? 'opacity-50' : ''}
        aria-busy={isPending}
      >
        <button
          className="btn-mint w-full text-base py-4"
          onClick={handleMarkComplete}
          disabled={isPending || isCompleted}
        >
          {isCompleted
            ? 'Session Completed ✓'
            : isPending
              ? 'Confirming...'
              : 'Mark My Sessions Complete →'}
        </button>
      </div>
    </section>
  );
}
