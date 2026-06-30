/**
 * "On Break" badge with return date for matched mentees viewing an unavailable mentor.
 *
 * Requirements: 32.3, 32.6, 32.9
 */

interface OnBreakBadgeProps {
  returnDate?: string;
  className?: string;
}

export function OnBreakBadge({ returnDate, className = '' }: OnBreakBadgeProps) {
  const formattedDate = returnDate
    ? new Date(returnDate).toLocaleDateString('en-AU', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      })
    : null;

  return (
    <span
      className={[
        'inline-flex items-center gap-1 rounded-full',
        'bg-warning/10 text-warning border border-warning/30',
        'text-xs px-2 py-0.5 font-medium',
        className,
      ].join(' ')}
      aria-label={
        formattedDate
          ? `Mentor is on break. Back on ${formattedDate}`
          : 'Mentor is on break'
      }
    >
      <svg
        className="h-3 w-3"
        fill="currentColor"
        viewBox="0 0 20 20"
        aria-hidden="true"
      >
        <path
          fillRule="evenodd"
          d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z"
          clipRule="evenodd"
        />
      </svg>
      On Break{formattedDate ? ` · Back ${formattedDate}` : ''}
    </span>
  );
}
