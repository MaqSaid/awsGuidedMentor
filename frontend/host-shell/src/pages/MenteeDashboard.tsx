import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ScoreRing } from '../components/ScoreRing';
import { DashboardSkeleton } from '../components/Skeleton';
import { useViewTransition } from '../hooks/useViewTransition';
import { usePrefetch } from '../hooks/usePrefetch';
import { apiUrl } from '../lib/api';

interface DashboardData {
  topMatch: number;
  activeSessions: number;
  goal: {
    title: string;
    description: string;
    category: string;
    locked: boolean;
    mentorAssigned: string;
  };
  sessions: Array<{
    sessionId: string;
    mentor: { displayName: string; initials: string; score: number };
    status: string;
  }>;
  mentorPreviews: Array<{
    mentorId: string;
    displayName: string;
    initials: string;
    professionalTitle: string;
    compatibilityScore: number;
  }>;
}

export default function MenteeDashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const transitionTo = useViewTransition();
  const { prefetch, cancelPrefetch } = usePrefetch();

  useEffect(() => {
    fetch(apiUrl('/v1/dashboard/mentee'))
      .then((r) => r.json())
      .then((d) => setData(d as DashboardData));
  }, []);

  if (!data) {
    return <DashboardSkeleton />;
  }

  return (
    <section className="max-w-5xl mx-auto px-4 md:px-6 py-6 md:py-10">
      <h1 className="text-2xl md:text-3xl font-bold mb-6 md:mb-8" style={{ fontFamily: 'Outfit, sans-serif' }}>
        Welcome back, James 👋
      </h1>

      {/* Stat cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-10">
        <div className="glass-card p-5 border-l-4 border-mint">
          <p className="text-sm text-text-secondary mb-1">Top Match</p>
          <p className="text-2xl font-bold text-mint">{data.topMatch}%</p>
        </div>

        <div className="glass-card p-5">
          <p className="text-sm text-text-secondary mb-1">Sessions Active</p>
          <p className="text-2xl font-bold text-text-primary">{data.activeSessions}</p>
        </div>

        <div className="glass-card p-5 border-l-4 border-amber">
          <p className="text-sm text-text-secondary mb-1">Goal</p>
          <p className="text-lg font-semibold text-text-primary">{data.goal.title}</p>
          {data.goal.locked && (
            <span className="inline-block mt-2 text-xs px-2 py-0.5 rounded-full bg-amber/20 text-amber font-medium">
              🔒 Mentor assigned
            </span>
          )}
        </div>
      </div>

      {/* Sessions */}
      <section aria-label="Active sessions" className="mb-10">
        <h2 className="text-xl font-semibold mb-4" style={{ fontFamily: 'Outfit, sans-serif' }}>
          Sessions
        </h2>
        <div className="space-y-3">
          {data.sessions.map((session) => (
            <div
              key={session.sessionId}
              className="glass-card p-4 md:p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
              onMouseEnter={() => prefetch(`/v1/sessions/${session.sessionId}/plan`)}
              onMouseLeave={cancelPrefetch}
            >
              <div className="flex items-center gap-3 md:gap-4">
                <div className="w-10 h-10 rounded-full bg-bg-secondary border border-border flex items-center justify-center text-sm font-semibold">
                  {session.mentor.initials}
                </div>
                <div>
                  <p className="font-medium text-text-primary">{session.mentor.displayName}</p>
                  <div className="flex items-center gap-2 mt-1">
                    <ScoreRing score={session.mentor.score} size="sm" />
                    <span className="text-xs px-2 py-0.5 rounded-full bg-mint/20 text-mint font-medium">
                      Active
                    </span>
                  </div>
                </div>
              </div>
              <button
                onClick={() => transitionTo(`/sessions/${session.sessionId}/plan`)}
                className="text-sm text-violet-light hover:text-violet font-medium transition-colors"
              >
                View Plan &rarr;
              </button>
            </div>
          ))}
        </div>
      </section>

      {/* Mentors preview */}
      <section aria-label="Browse mentors">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold" style={{ fontFamily: 'Outfit, sans-serif' }}>
            Mentors
          </h2>
          <Link to="/browse" className="text-sm text-violet-light hover:text-violet transition-colors">
            Browse All &rarr;
          </Link>
        </div>

        <div className="bg-amber/10 border border-amber/30 rounded-xl px-4 py-3 mb-4 text-sm text-amber">
          Browse Only — active mentor assigned
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {data.mentorPreviews.map((mentor) => (
            <div key={mentor.mentorId} className="glass-card p-4 text-center">
              <div className="w-12 h-12 rounded-full bg-bg-secondary border border-border flex items-center justify-center text-sm font-semibold mx-auto mb-2">
                {mentor.initials}
              </div>
              <p className="font-medium text-text-primary text-sm">{mentor.displayName}</p>
              <p className="text-xs text-text-muted mt-1">{mentor.professionalTitle}</p>
            </div>
          ))}
        </div>
      </section>
    </section>
  );
}
