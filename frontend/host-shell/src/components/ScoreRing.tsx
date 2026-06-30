interface ScoreRingProps {
  score: number;
  size?: 'sm' | 'md' | 'lg';
}

const sizeMap = {
  sm: { width: 48, stroke: 4, fontSize: 'text-xs' },
  md: { width: 72, stroke: 5, fontSize: 'text-base' },
  lg: { width: 120, stroke: 6, fontSize: 'text-2xl' },
} as const;

export function ScoreRing({ score, size = 'md' }: ScoreRingProps) {
  const { width, stroke, fontSize } = sizeMap[size];
  const radius = (width - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const progress = (score / 100) * circumference;
  const offset = circumference - progress;

  const colorClass =
    score > 80 ? 'text-mint' : score >= 60 ? 'text-violet-light' : 'text-amber';

  const strokeColor =
    score > 80 ? 'var(--color-mint)' : score >= 60 ? 'var(--color-violet-light)' : 'var(--color-amber)';

  return (
    <div
      className="relative inline-flex items-center justify-center"
      style={{ width, height: width }}
      role="img"
      aria-label={`Compatibility score: ${score}%`}
    >
      <svg
        width={width}
        height={width}
        className="transform -rotate-90"
      >
        <circle
          cx={width / 2}
          cy={width / 2}
          r={radius}
          fill="none"
          stroke="rgba(255,255,255,0.08)"
          strokeWidth={stroke}
        />
        <circle
          cx={width / 2}
          cy={width / 2}
          r={radius}
          fill="none"
          stroke={strokeColor}
          strokeWidth={stroke}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          className="transition-all duration-700 ease-out"
        />
      </svg>
      <span className={`absolute font-semibold ${fontSize} ${colorClass}`}>
        {score}
      </span>
    </div>
  );
}
