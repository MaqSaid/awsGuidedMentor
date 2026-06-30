import { useEffect, useState, useRef, memo } from 'react';
import { Link } from 'react-router-dom';
import { useVirtualizer } from '@tanstack/react-virtual';
import { ScoreRing } from '../components/ScoreRing';
import { DashboardSkeleton } from '../components/Skeleton';

interface MenteeCardData {
  menteeId: string;
  displayName: string;
  initials: string;
  goal: string;
  score: number;
  status: 'awaiting_confirmation' | 'active';
  sessionId: string;
}

interface MentorDashData {
  menteeCount: number;
  sessionCount: number;
  completedCount: number;
  mentees: MenteeCardData[];
}

const MenteeCard = memo(function MenteeCard({ mentee }: { mentee: MenteeCardData }) {
  return (
    <div className="glass-card p-6 flex items-center justify-between">
      <div className="flex items-center gap-4">
        <div className="w-14 h-14 rounded-full bg-violet/20 border-2 border-violet/40 flex items-center justify-center text-lg font-bold text-violet-light">
          {mentee.initials}
        </div>
        <div>
          <p className="font-semibold text-text-primary">{mentee.displayName}</p>
          <p className="text-sm text-text-secondary mt-0.5">{mentee.goal}</p>
          <div className="flex items-center gap-2 mt-2">
            <ScoreRing score={mentee.score} size="sm" />
            {mentee.status === 'awaiting_confirmation' ? (
              <span className="text-xs px-2 py-0.5 rounded-full bg-amber/20 text-amber font-medium">
                ⚠ Awaiting Confirmation
              </span>
            ) : (
              <span className="text-xs px-2 py-0.5 rounded-full bg-mint/20 text-mint font-medium">
                Active
              </span>
            )}
          </div>
        </div>
      </div>
      <div>
        {mentee.status === 'awaiting_confirmation' ? (
          <Link
            to={`/sessions/${mentee.sessionId}/plan`}
            className="text-sm text-amber hover:text-amber font-medium transition-colors"
          >
            Confirm Now &rarr;
          </Link>
        ) : (
          <Link
            to={`/sessions/${mentee.sessionId}/plan`}
            className="text-sm text-violet-light hover:text-violet font-medium transition-colors"
          >
            View Plan &rarr;
          </Link>
        )}
      </div>
    </div>
  );
});

export default function MentorDashboard() {
  const [data, setData] = useState<MentorDashData | null>(null);
  const parentRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    fetch('/v1/dashboard/mentor')
      .then((r) => r.json())
      .then((d) => setData(d as MentorDashData));
  }, []);

  const mentees = data?.mentees ?? [];

  const rowVirtualizer = useVirtualizer({
    count: mentees.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 200,
    overscan: 3,
  });

  if (!data) {
    return <DashboardSkeleton />;
  }

  return (
    <section className="max-w-5xl mx-auto px-6 py-10">
      <h1 className="text-3xl font-bold mb-8" style={{ fontFamily: 'Outfit, sans-serif' }}>
        Welcome back, Marcus 👋
      </h1>

      {/* Stat cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-10">
        <div className="glass-card p-5">
          <p className="text-sm text-text-secondary mb-1">Mentees</p>
          <p className="text-2xl font-bold text-text-primary">{data.menteeCount}</p>
        </div>
        <div className="glass-card p-5">
          <p className="text-sm text-text-secondary mb-1">Sessions</p>
          <p className="text-2xl font-bold text-text-primary">{data.sessionCount}</p>
        </div>
        <div className="glass-card p-5">
          <p className="text-sm text-text-secondary mb-1">Completed</p>
          <p className="text-2xl font-bold text-mint">{data.completedCount}</p>
        </div>
      </div>

      {/* Mentees */}
      <section aria-label="Your mentees">
        <h2 className="text-xl font-semibold mb-4" style={{ fontFamily: 'Outfit, sans-serif' }}>
          Your Mentees
        </h2>

        {mentees.length > 6 ? (
          <div ref={parentRef} className="h-[600px] overflow-auto rounded-xl">
            <div style={{ height: `${rowVirtualizer.getTotalSize()}px`, position: 'relative' }}>
              {rowVirtualizer.getVirtualItems().map((virtualItem) => {
                const mentee = mentees[virtualItem.index]!;
                return (
                  <div
                    key={virtualItem.key}
                    style={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      width: '100%',
                      transform: `translateY(${virtualItem.start}px)`,
                    }}
                  >
                    <MenteeCard mentee={mentee} />
                  </div>
                );
              })}
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            {mentees.map((mentee) => (
              <MenteeCard key={mentee.menteeId} mentee={mentee} />
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
