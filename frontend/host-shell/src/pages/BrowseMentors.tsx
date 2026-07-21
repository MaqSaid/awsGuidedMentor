import { useEffect, useState, useDeferredValue, useMemo, memo } from 'react';
import { ScoreRing } from '../components/ScoreRing';
import { BrowseSkeleton } from '../components/Skeleton';
import { apiUrl } from '../lib/api';

interface Mentor {
  mentorId: string;
  displayName: string;
  title: string;
  chapter: string;
  expertiseAreas: string[];
  compatibilityScore: number;
  hasActiveOpportunities: boolean;
  availabilityStatus: string;
}

interface MentorsResponse {
  mentors: Mentor[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface MentorCardProps {
  mentor: Mentor;
  isSelected: boolean;
  hasActiveMentor: boolean;
  onSelect: (id: string) => void;
}

const MentorCard = memo(function MentorCard({ mentor, isSelected, hasActiveMentor, onSelect }: MentorCardProps) {
  const isAtCapacity = mentor.availabilityStatus === 'at_capacity';
  const initials = mentor.displayName.split(' ').map(n => n[0]).join('').toUpperCase();

  return (
    <article className="glass-card p-6 flex flex-col items-center text-center transition-all duration-200">
      {/* Avatar */}
      <div className="w-16 h-16 rounded-full bg-bg-secondary border border-border flex items-center justify-center text-lg font-bold text-text-primary mb-3">
        {initials}
      </div>

      {/* Name & title */}
      <h3 className="font-semibold text-text-primary">{mentor.displayName}</h3>
      <p className="text-sm text-text-secondary mt-1">
        {mentor.title} &bull; {mentor.chapter}
      </p>

      {/* Skill chips */}
      <div className="flex flex-wrap gap-1.5 justify-center mt-3">
        {mentor.expertiseAreas.map((skill) => (
          <span
            key={skill}
            className="text-xs px-2 py-0.5 rounded-full bg-white/5 border border-border text-text-secondary"
          >
            {skill}
          </span>
        ))}
      </div>

      {/* Score ring */}
      <div className="mt-4">
        <ScoreRing score={mentor.compatibilityScore} size="md" />
      </div>

      {/* Action button */}
      <div className="mt-4 w-full">
        {isSelected ? (
          <button
            disabled
            className="w-full py-2.5 rounded-xl bg-mint/20 text-mint font-medium text-sm cursor-default"
          >
            Selected ✓
          </button>
        ) : isAtCapacity || hasActiveMentor ? (
          <button
            disabled
            className="w-full py-2.5 rounded-xl bg-white/5 text-text-muted font-medium text-sm cursor-not-allowed border border-border"
          >
            🔒 Connect
          </button>
        ) : (
          <button
            onClick={() => onSelect(mentor.mentorId)}
            className="btn-mint w-full text-sm"
          >
            Connect
          </button>
        )}
      </div>
    </article>
  );
});

export default function BrowseMentors() {
  const [data, setData] = useState<MentorsResponse | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const deferredQuery = useDeferredValue(searchQuery);
  const isStale = searchQuery !== deferredQuery;
  const hasActiveMentor = true; // Demo: user already has a mentor

  useEffect(() => {
    const token = localStorage.getItem('gm_access_token');
    const headers: HeadersInit = token
      ? { 'Authorization': `Bearer ${token}` }
      : {};
    fetch(apiUrl('/v1/mentors'), { headers })
      .then((r) => r.json())
      .then((d) => setData(d as MentorsResponse));
  }, []);

  const filteredMentors = useMemo(() => {
    if (!data) return [];
    if (!deferredQuery.trim()) return data.mentors;
    const q = deferredQuery.toLowerCase();
    return data.mentors.filter((m) =>
      m.displayName.toLowerCase().includes(q) ||
      m.expertiseAreas.some((s) => s.toLowerCase().includes(q)) ||
      m.title.toLowerCase().includes(q)
    );
  }, [data, deferredQuery]);

  if (!data) {
    return <BrowseSkeleton />;
  }

  return (
    <section className="max-w-6xl mx-auto px-6 py-10">
      <h1 className="text-3xl font-bold mb-2" style={{ fontFamily: 'Outfit, sans-serif' }}>
        AWS Community Mentors
      </h1>
      <p className="text-text-secondary mb-6">
        {data.totalCount} mentors available
      </p>

      {/* Locked banner */}
      {hasActiveMentor && (
        <div className="bg-amber/10 border border-amber/30 rounded-xl px-4 py-3 mb-6 text-sm text-amber">
          🔒 You already have an active mentor — browse only mode
        </div>
      )}

      {/* Search input with deferred value */}
      <div className="relative mb-6">
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search by name, skill, or company..."
          className="w-full px-4 py-3 rounded-xl bg-bg-card border border-border text-text-primary placeholder:text-text-muted focus:outline-none focus:border-violet/50 focus:ring-1 focus:ring-violet/30 transition-colors"
          aria-label="Search mentors"
        />
        {isStale && (
          <div className="absolute right-4 top-1/2 -translate-y-1/2">
            <div className="w-4 h-4 border-2 border-violet border-t-transparent rounded-full animate-spin" />
          </div>
        )}
      </div>

      {/* Mentor grid */}
      <div className={`grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 transition-opacity duration-200 ${isStale ? 'opacity-60' : 'opacity-100'}`}>
        {filteredMentors.map((mentor) => (
          <MentorCard
            key={mentor.mentorId}
            mentor={mentor}
            isSelected={selectedId === mentor.mentorId}
            hasActiveMentor={hasActiveMentor}
            onSelect={setSelectedId}
          />
        ))}
      </div>

      {filteredMentors.length === 0 && deferredQuery.trim() && (
        <p className="text-center text-text-secondary mt-8">
          No mentors found matching &ldquo;{deferredQuery}&rdquo;
        </p>
      )}
    </section>
  );
}
