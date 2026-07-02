/**
 * OpportunitiesPage — Browse all active postings with filters
 * (type[job/workshop/event/training], location, skills, experience),
 * contextual action button ("Apply" for jobs, "Register" for events/workshops/training).
 *
 * Requirements: 28.1, 28.5, 28.6, 28.7, 28.8, 28.13
 */
import { useState } from 'react';
import { Skeleton, Button } from '@guided-mentor/design-system';
import { useOpportunities, useMentorOpportunities } from '../api/mentoring-api';
import { OpportunityPostingCard } from '../components/OpportunityPostingCard';
import { OpportunityFilterPanel } from '../components/OpportunityFilterPanel';
import { OpportunityPostingForm } from '../components/OpportunityPostingForm';
import type { OpportunityFilters, OpportunityPosting } from '../types';

export function OpportunitiesPage() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<OpportunityFilters>({});
  const [showPostForm, setShowPostForm] = useState(false);
  const [editPosting, setEditPosting] = useState<OpportunityPosting | null>(null);
  const [showMobileFilters, setShowMobileFilters] = useState(false);

  const { data, isLoading, isError, refetch } = useOpportunities(page, filters);
  const { data: myPostings } = useMentorOpportunities();

  const totalPages = data?.totalPages ?? 1;

  const handleEdit = (posting: OpportunityPosting) => {
    setEditPosting(posting);
    setShowPostForm(true);
  };

  const handleCloseForm = () => {
    setShowPostForm(false);
    setEditPosting(null);
  };

  return (
    <div className="max-w-7xl mx-auto px-4 md:px-6 py-6" data-testid="mentoring-opportunities-page">
      <div className="flex items-center justify-between mb-6 flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-text-primary">Opportunities</h1>
        <div className="flex items-center gap-2">
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
          {myPostings !== undefined && (
            <Button
              variant="primary"
              size="sm"
              onClick={() => setShowPostForm(true)}
              disabled={(myPostings?.filter((p) => p.isActive).length ?? 0) >= 5}
            >
              Post Opportunity
            </Button>
          )}
        </div>
      </div>

      {/* Mentor's own postings */}
      {myPostings && myPostings.length > 0 && (
        <div className="mb-8">
          <h2 className="text-lg font-semibold text-text-primary mb-3">Your Postings</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 mb-2">
            {myPostings.map((posting) => (
              <div key={posting.postingId} className="relative">
                <OpportunityPostingCard posting={posting} />
                <button
                  onClick={() => handleEdit(posting)}
                  className="absolute top-2 right-2 text-xs text-primary hover:underline focus-visible:ring-2 focus-visible:ring-primary outline-none rounded z-10 bg-surface/80 px-2 py-0.5"
                  aria-label={`Edit ${posting.title}`}
                >
                  Edit
                </button>
              </div>
            ))}
          </div>
          <p className="text-xs text-text-muted">
            {myPostings.filter((p) => p.isActive).length}/5 active postings
          </p>
        </div>
      )}

      <div className="flex gap-6">
        {/* Sidebar filters */}
        <OpportunityFilterPanel
          filters={filters}
          onFiltersChange={setFilters}
          className="w-64 shrink-0 hidden lg:block"
        />

        {/* Main content */}
        <div className="flex-1 min-w-0">
          {/* Loading */}
          {isLoading && (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {Array.from({ length: 12 }).map((_, i) => (
                <Skeleton key={i} height="12rem" className="rounded-lg" />
              ))}
            </div>
          )}

          {/* Error */}
          {isError && (
            <div className="glass-card p-8 rounded-lg text-center">
              <p className="text-text-secondary mb-4">Failed to load opportunities.</p>
              <Button variant="secondary" onClick={() => refetch()}>
                Retry
              </Button>
            </div>
          )}

          {/* Empty state */}
          {!isLoading && !isError && data?.items.length === 0 && (
            <div className="glass-card p-8 rounded-lg text-center">
              <p className="text-text-primary font-medium mb-2">No opportunities available</p>
              <p className="text-text-secondary text-sm">
                Try adjusting your filters or check back later for new postings.
              </p>
            </div>
          )}

          {/* Opportunity grid */}
          {!isLoading && !isError && data && data.items.length > 0 && (
            <>
              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
                {data.items.map((posting) => (
                  <OpportunityPostingCard key={posting.postingId} posting={posting} />
                ))}
              </div>

              {/* Pagination */}
              <nav
                className="flex items-center justify-center gap-2 mt-8"
                aria-label="Opportunities pagination"
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

      {/* Create/Edit posting form */}
      <OpportunityPostingForm
        open={showPostForm}
        onClose={handleCloseForm}
        editPosting={editPosting}
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
            <OpportunityFilterPanel
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

export default OpportunitiesPage;
