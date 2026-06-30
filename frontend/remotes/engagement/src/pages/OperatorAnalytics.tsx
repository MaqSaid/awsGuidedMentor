/**
 * OperatorAnalytics — Admin-only analytics dashboard with DAU/WAU/MAU charts,
 * feature heatmap, funnels, and error hotspots.
 *
 * Requirements: 30.6
 */
import { useAnalyticsMetrics, useFeatureHeatmap, useFunnels, useErrorHotspots } from '../hooks/useApi';
import type { FeatureHeatmapItem, FunnelStep, ErrorHotspot } from '../types';

export function OperatorAnalytics() {
  const { data: metrics, isLoading: metricsLoading, isError: metricsError } = useAnalyticsMetrics();
  const { data: heatmap, isLoading: heatmapLoading } = useFeatureHeatmap();
  const { data: funnels, isLoading: funnelsLoading } = useFunnels();
  const { data: errors, isLoading: errorsLoading } = useErrorHotspots();

  const isLoading = metricsLoading || heatmapLoading || funnelsLoading || errorsLoading;

  if (isLoading) {
    return (
      <div data-testid="engagement-operator-analytics" className="p-6 space-y-6">
        <h1 className="text-2xl font-bold text-text-primary">Platform Analytics</h1>
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="glass-card p-4 animate-pulse h-32 rounded-lg bg-[rgba(255,255,255,0.06)]" role="status" aria-label="Loading" />
          ))}
        </div>
      </div>
    );
  }

  if (metricsError) {
    return (
      <div data-testid="engagement-operator-analytics" className="p-6 space-y-6">
        <h1 className="text-2xl font-bold text-text-primary">Platform Analytics</h1>
        <div className="glass-card p-6 text-center" role="alert">
          <p className="text-text-secondary mb-4">Failed to load analytics data.</p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-primary text-[#0a1628] font-semibold rounded-md hover:opacity-90 transition-opacity"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div data-testid="engagement-operator-analytics" className="p-6 space-y-6">
      <h1 className="text-2xl font-bold text-text-primary">Platform Analytics</h1>

      {/* Active Users Summary */}
      <section aria-label="Active users metrics">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Active Users</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <MetricCard label="Daily Active Users (DAU)" value={metrics?.dau ?? 0} />
          <MetricCard label="Weekly Active Users (WAU)" value={metrics?.wau ?? 0} />
          <MetricCard label="Monthly Active Users (MAU)" value={metrics?.mau ?? 0} />
        </div>
      </section>

      {/* DAU/WAU/MAU Charts */}
      {metrics && (
        <section aria-label="User activity charts">
          <h2 className="text-lg font-semibold text-text-primary mb-3">Activity Trends</h2>
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <MiniChart title="DAU (30 days)" data={metrics.dauHistory} />
            <MiniChart title="WAU (12 weeks)" data={metrics.wauHistory} />
            <MiniChart title="MAU (12 months)" data={metrics.mauHistory} />
          </div>
        </section>
      )}

      {/* Feature Heatmap */}
      <section aria-label="Feature usage heatmap">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Feature Usage Heatmap</h2>
        {heatmap && heatmap.length > 0 ? (
          <div className="glass-card p-4 rounded-lg">
            <div className="space-y-3">
              {heatmap.map((item) => (
                <HeatmapRow key={item.feature} item={item} />
              ))}
            </div>
          </div>
        ) : (
          <p className="text-text-muted text-sm">No feature usage data available.</p>
        )}
      </section>

      {/* User Funnels */}
      <section aria-label="User flow funnels">
        <h2 className="text-lg font-semibold text-text-primary mb-3">User Funnel</h2>
        {funnels && funnels.length > 0 ? (
          <div className="glass-card p-4 rounded-lg">
            <div className="space-y-2">
              {funnels.map((step, index) => (
                <FunnelRow key={step.step} step={step} index={index} total={funnels[0]?.count ?? 0} />
              ))}
            </div>
          </div>
        ) : (
          <p className="text-text-muted text-sm">No funnel data available.</p>
        )}
      </section>

      {/* Error Hotspots */}
      <section aria-label="Error hotspots">
        <h2 className="text-lg font-semibold text-text-primary mb-3">Error Hotspots</h2>
        {errors && errors.length > 0 ? (
          <div className="glass-card rounded-lg overflow-hidden">
            <table className="w-full text-sm" aria-label="Error hotspot table">
              <thead>
                <tr className="border-b border-[rgba(255,255,255,0.1)]">
                  <th className="text-left p-3 text-text-muted font-medium">Page</th>
                  <th className="text-right p-3 text-text-muted font-medium">Errors</th>
                  <th className="text-right p-3 text-text-muted font-medium">Error Rate</th>
                </tr>
              </thead>
              <tbody>
                {errors.map((hotspot) => (
                  <ErrorRow key={hotspot.page} hotspot={hotspot} />
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-text-muted text-sm">No error data available.</p>
        )}
      </section>
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="glass-card p-4 rounded-lg">
      <p className="text-xs text-text-muted mb-1">{label}</p>
      <p className="text-3xl font-bold text-text-primary">{value.toLocaleString()}</p>
    </div>
  );
}

function MiniChart({ title, data }: { title: string; data: { date: string; count: number }[] }) {
  if (!data || data.length === 0) return null;

  const maxVal = Math.max(...data.map((d) => d.count), 1);

  return (
    <div className="glass-card p-4 rounded-lg">
      <h3 className="text-xs text-text-muted mb-3">{title}</h3>
      <div className="flex items-end gap-px h-16" role="img" aria-label={`${title} chart`}>
        {data.map((point, idx) => (
          <div
            key={idx}
            className="flex-1 bg-primary/60 rounded-t-sm hover:bg-primary transition-colors"
            style={{ height: `${(point.count / maxVal) * 100}%` }}
            title={`${point.date}: ${point.count}`}
          />
        ))}
      </div>
      <div className="flex justify-between mt-2">
        <span className="text-xs text-text-muted">{data[0]?.date}</span>
        <span className="text-xs text-text-muted">{data[data.length - 1]?.date}</span>
      </div>
    </div>
  );
}

function HeatmapRow({ item }: { item: FeatureHeatmapItem }) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-sm text-text-primary w-40 truncate">{item.feature}</span>
      <div className="flex-1 h-3 rounded-full bg-[rgba(255,255,255,0.06)] overflow-hidden">
        <div
          className="h-full rounded-full bg-primary transition-all"
          style={{ width: `${item.percentage}%` }}
        />
      </div>
      <span className="text-xs text-text-muted w-20 text-right">
        {item.usageCount.toLocaleString()} ({item.percentage}%)
      </span>
    </div>
  );
}

function FunnelRow({ step, index, total }: { step: FunnelStep; index: number; total: number }) {
  const percent = total > 0 ? Math.round((step.count / total) * 100) : 0;

  return (
    <div className="flex items-center gap-3">
      <span className="text-xs text-text-muted w-6">{index + 1}.</span>
      <span className="text-sm text-text-primary w-36 truncate">{step.step}</span>
      <div className="flex-1 h-6 rounded bg-[rgba(255,255,255,0.06)] overflow-hidden relative">
        <div
          className="h-full bg-primary/40 rounded transition-all"
          style={{ width: `${percent}%` }}
        />
        <span className="absolute inset-0 flex items-center px-2 text-xs text-text-primary">
          {step.count.toLocaleString()}
        </span>
      </div>
      <span className="text-xs text-text-muted w-14 text-right">{percent}%</span>
      {step.dropoff > 0 && (
        <span className="text-xs text-error w-16 text-right">-{step.dropoff}%</span>
      )}
    </div>
  );
}

function ErrorRow({ hotspot }: { hotspot: ErrorHotspot }) {
  return (
    <tr className="border-b border-[rgba(255,255,255,0.05)] hover:bg-[rgba(255,255,255,0.02)]">
      <td className="p-3 text-text-primary">{hotspot.page}</td>
      <td className="p-3 text-right text-text-secondary">{hotspot.errorCount}</td>
      <td className="p-3 text-right">
        <span className={hotspot.errorRate > 5 ? 'text-error font-medium' : 'text-text-secondary'}>
          {hotspot.errorRate.toFixed(1)}%
        </span>
      </td>
    </tr>
  );
}

export default OperatorAnalytics;
