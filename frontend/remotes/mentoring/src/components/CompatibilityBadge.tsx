/**
 * CompatibilityBadge — colour-coded score display
 * Green >80%, Orange 50-79%, Red <50%
 *
 * Requirements: 5.9, 6.1
 */

interface CompatibilityBadgeProps {
  score: number;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

function getScoreColor(score: number): string {
  if (score > 80) return 'text-success bg-success/10 border-success/30';
  if (score >= 50) return 'text-warning bg-warning/10 border-warning/30';
  return 'text-error bg-error/10 border-error/30';
}

function getScoreLabel(score: number): string {
  if (score > 80) return 'Excellent match';
  if (score >= 50) return 'Good match';
  return 'Low match';
}

const sizeClasses = {
  sm: 'text-xs px-2 py-0.5',
  md: 'text-sm px-3 py-1',
  lg: 'text-base px-4 py-1.5 font-semibold',
};

export function CompatibilityBadge({ score, size = 'md', className = '' }: CompatibilityBadgeProps) {
  return (
    <span
      className={[
        'inline-flex items-center rounded-full border font-medium',
        sizeClasses[size],
        getScoreColor(score),
        className,
      ].join(' ')}
      aria-label={`${score}% match — ${getScoreLabel(score)}`}
    >
      {score}% match
    </span>
  );
}
