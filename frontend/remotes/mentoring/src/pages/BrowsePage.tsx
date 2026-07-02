/**
 * BrowsePage — Paginated mentor cards (12/page) with CompatibilityBadge,
 * expertise tags, availability summary, "Sharing Opportunities" badge,
 * keyboard navigation between cards.
 *
 * Requirements: 5.9, 6.1, 6.7, 6.8
 */
import { useState, useRef, useCallback } from 'react';
import { Skeleton, Button } from '@guided-mentor/design-system';
import { useBrowseMentors, useAcquireLock, useReleaseLock, useConfirmSelection } from '../api/mentoring-api';
import { MentorCard } from '../components/MentorCard';
import { FilterPanel } from '../components/FilterPanel';
import { LockConfirmModal } from '../components/LockConfirmModal';
import type { BrowseFilters, LockResult } from '../types';

export function BrowsePage() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<BrowseFilters>({});
  const [activeLock, setActiveLock] = useState<LockResult | null>(null);
  const [lockedMentorName, setLockedMentorName] = useState('');
  const [showMobileFilters, setShowMobileFilters] = useState(false);
  const cardRefs = useRef<Map<number, HTMLDivElement>>(new Map());

  const { data, isLoading, isError, refetch } = useBrowseMentors(page, filters);
  const acquireLock = useAcquireLock();
  const releaseLock = useReleaseLock();
  const confirmSelection = useConfirmSelection();

  const handleSelectMentor = useCallback(
    (mentorId: string) => {
      const mentor = data?.items.find((m) => m.mentorId === mentorId);
      if (!mentor) return;

      acquireLock.mutate(
        { mentorId },
        {
          onSuccess: (lock) => {
            setActiveLock(lock);
            setLockedMentorName(mentor.displayName);
          },
        }
      );
    },
    [acquireLock, data?.items]
  );

  const handleConfirm = useCallback(() => {
    if (!activeLock) return;
    confirmSelection.mutate(
      { lockId: activeLock.lockId, mentorId: activeLock.mentorId },
      {
        onSuccess: () => setActiveLock(null),
      }
    );
  }, [activeLock, confirmSelection]);

  const handleCancel = useCallback(() => {
    if (!activeLock) return;
    releaseLock.mutate(
      { lockId: activeLock.lockId },
      {
        onSuccess: () => setActiveLock(null),
      }
    );
  }, [activeLock, releaseLock]);

  // Keyboard navigation: Arrow keys between cards
  const handleCardKeyDown = useCallback(
    (e: React.KeyboardEvent, index: number) => {
      const items = data?.items ?? [];
      let targetIndex: number | null = null;

      if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
        e.preventDefault();
        targetIndex = index < items.length - 1 ? index + 1 : 0;
      } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
        e.preventDefault();
        targetIndex = index > 0 ? index - 1 : items.length - 1;
      }

      if (targetIndex !== null) {
        cardRefs.current.get(targetIndex)?.focus();
      }
    },
    [data?.items]
  );

  const totalPages = data?.totalPages ?? 1;

  return (
    <div className="max-w-7xl mx-auto px-4 md:px-6 py-6" data-testid="mentoring-browse-page">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-text-primary">Browse Mentors</h1>
        {/* Mobile filter button */}
        <button
          className="lg:hidden btn-ghost flex items-center gap-2 text-sm"
          onClick={() => setShowMobileFilters(true)}
          aria-label="Open filters"
        >
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" className="text-text-secondary" aria-hidden="true">
            <path d="M2 4h12M4 8h8M6 12h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
          </svg>
          Filters
        </button>
      </div>

      <div className="flex gap-6">
        {/* Sidebar filters */}
        <FilterPanel
          filters={filters}
          onFiltersChange={setFilters}
          className="w-64 shrink-0 hidden lg:block"
        />

        {/* Main content */}
        <div className="flex-1 min-w-0">
          {/* Loading state */}
          {isLoading && (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {Array.from({ length: 12 }).map((_, i) => (
                <Skeleton key={i} height="14rem" className="rounded-lg" />
              ))}
            </div>
          )}

          {/* Error state */}
          {isError && (
            <div className="glass-card p-8 rounded-lg text-center">
              <p className="text-text-secondary mb-4">Failed to load mentors.</p>
              <Button variant="secondary" onClick={() => refetch()}>
                Retry
              </Button>
            </div>
          )}

          {/* Empty state */}
          {!isLoading && !isError && data?.items.length === 0 && (
            <div className="glass-card p-8 rounded-lg text-center">
              <p className="text-text-primary font-medium mb-2">No mentors available</p>
              <p className="text-text-secondary text-sm">
                Try adjusting your filters or check back later when more mentors are available.
              </p>
            </div>
          )}

          {/* Mentor card grid */}
          {!isLoading && !isError && data && data.items.length > 0 && (
            <>
              <div
                className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4"
                role="list"
                aria-label="Available mentors"
              >
                {data.items.map((mentor, index) => (
                  <div
                    key={mentor.mentorId}
                    role="listitem"
                    onKeyDown={(e) => handleCardKeyDown(e, index)}
                  >
                    <MentorCard
                      ref={(el) => {
                        if (el) cardRefs.current.set(index, el);
                        else cardRefs.current.delete(index);
                      }}
                      mentor={mentor}
                      isLocked={mentor.availabilityStatus === 'unavailable'}
                      onSelect={handleSelectMentor}
                    />
                  </div>
                ))}
              </div>

              {/* Pagination */}
              <nav
                className="flex items-center justify-center gap-2 mt-8"
                aria-label="Browse pagination"
              >
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  aria-label="Previous page"
                >
                  ← Previous
                </Button>
                <span className="text-sm text-text-secondary px-3">
                  Page {page} of {totalPages}
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  aria-label="Next page"
                >
                  Next →
                </Button>
              </nav>
            </>
          )}
        </div>
      </div>

      {/* Lock confirmation modal */}
      <LockConfirmModal
        open={activeLock !== null}
        mentorName={lockedMentorName}
        expiresAt={activeLock?.expiresAt ?? ''}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
        confirmLoading={confirmSelection.isPending}
        cancelLoading={releaseLock.isPending}
      />

      {/* Mobile filter drawer */}
      {showMobileFilters && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setShowMobileFilters(false)}
            aria-hidden="true"
          />
          <div className="absolute bottom-0 left-0 right-0 max-h-[80vh] overflow-y-auto glass-card rounded-t-2xl p-6 safe-bottom animate-[slideUp_200ms_ease-out]">
            <div className="flex justify-between items-center mb-4">
              <h3 className="font-semibold text-text-primary">Filters</h3>
              <button
                onClick={() => setShowMobileFilters(false)}
                className="min-w-[44px] min-h-[44px] flex items-center justify-center text-text-muted hover:text-text-primary"
                aria-label="Close filters"
              >
                <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                  <path d="M15 5L5 15M5 5l10 10" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                </svg>
              </button>
            </div>
            <FilterPanel
              filters={filters}
              onFiltersChange={setFilters}
              className="w-full"
            />
            <button
              className="btn-violet w-full mt-4"
              onClick={() => setShowMobileFilters(false)}
            >
              Apply Filters
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default BrowsePage;
