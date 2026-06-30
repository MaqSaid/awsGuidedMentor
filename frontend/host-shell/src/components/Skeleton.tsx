interface SkeletonProps {
  className?: string;
  variant?: 'text' | 'circle' | 'card' | 'stat';
  count?: number;
}

export function Skeleton({ className = '', variant = 'text', count = 1 }: SkeletonProps) {
  const base = 'animate-pulse bg-white/5 rounded-xl';

  const variants = {
    text: `${base} h-4 w-full`,
    circle: `${base} w-14 h-14 rounded-full`,
    card: `${base} h-48 w-full`,
    stat: `${base} h-24 w-full`,
  };

  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className={`${variants[variant]} ${className}`} aria-hidden="true" />
      ))}
    </>
  );
}

// Pre-built skeleton layouts matching actual page structures
export function DashboardSkeleton() {
  return (
    <div className="max-w-5xl mx-auto px-6 py-10 space-y-8">
      <Skeleton variant="text" className="h-8 w-64" />
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Skeleton variant="stat" />
        <Skeleton variant="stat" />
        <Skeleton variant="stat" />
      </div>
      <Skeleton variant="text" className="h-6 w-40" />
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        <Skeleton variant="card" />
        <Skeleton variant="card" />
        <Skeleton variant="card" />
      </div>
    </div>
  );
}

export function BrowseSkeleton() {
  return (
    <div className="max-w-6xl mx-auto px-6 py-10 space-y-6">
      <Skeleton variant="text" className="h-8 w-72" />
      <Skeleton variant="text" className="h-4 w-48" />
      <Skeleton variant="text" className="h-12 w-full" />
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} variant="card" className="h-72" />
        ))}
      </div>
    </div>
  );
}

export function SessionPlanSkeleton() {
  return (
    <div className="max-w-4xl mx-auto px-6 py-10 space-y-8">
      <div className="flex items-center justify-between">
        <Skeleton variant="circle" />
        <Skeleton variant="circle" className="w-24 h-24" />
        <Skeleton variant="circle" />
      </div>
      <Skeleton variant="text" className="h-4 w-full" />
      <Skeleton variant="text" className="h-6 w-56" />
      {Array.from({ length: 5 }).map((_, i) => (
        <Skeleton key={i} variant="text" className="h-20 w-full" />
      ))}
    </div>
  );
}
