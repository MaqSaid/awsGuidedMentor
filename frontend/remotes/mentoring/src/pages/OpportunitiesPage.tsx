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
    <div className="max-w-7xl mx-auto p-6" data-testid="mentoring-opportunities-page">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-text-primary">Opportunities</h1>
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
    </div>
  );
}

export default OpportunitiesPage;
