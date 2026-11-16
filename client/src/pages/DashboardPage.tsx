import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { FINDING_SEVERITIES, INSPECTION_STATUSES } from '../api/types';
import { ErrorMessage, Loading } from '../components/AsyncStatus';
import { SeverityBadge } from '../components/SeverityBadge';
import { StatCard } from '../components/StatCard';
import { StatusBadge } from '../components/StatusBadge';
import { useAsync } from '../hooks/useAsync';
import { formatDateTime, statusLabel } from '../utils/format';

const statusTone = {
  Scheduled: 'info',
  InProgress: 'warning',
  Completed: 'success',
  Failed: 'danger',
} as const;

export function DashboardPage() {
  const { data, error, loading, reload } = useAsync(() => api.dashboard.summary(), []);

  if (loading && !data) return <Loading />;
  if (error) return <ErrorMessage error={error} onRetry={reload} />;
  if (!data) return null;

  const maxSeverity = Math.max(1, ...FINDING_SEVERITIES.map((s) => data.openFindingsBySeverity[s]));

  return (
    <section>
      <h1>Dashboard</h1>

      <div className="stat-grid">
        <StatCard label="Sites" value={data.totalSites} />
        <StatCard label="Inspections" value={data.totalInspections} />
        <StatCard label="Open findings" value={data.openFindings} tone="warning" hint={`${data.totalFindings} recorded`} />
        <StatCard label="Critical open" value={data.openFindingsBySeverity.Critical} tone="danger" />
      </div>

      <h2>Inspections by status</h2>
      <div className="stat-grid">
        {INSPECTION_STATUSES.map((status) => (
          <StatCard key={status} label={statusLabel(status)} value={data.inspectionsByStatus[status]} tone={statusTone[status]} />
        ))}
      </div>

      <div className="two-column">
        <div className="panel">
          <h2>Open findings by severity</h2>
          <ul className="bar-list">
            {[...FINDING_SEVERITIES].reverse().map((severity) => {
              const count = data.openFindingsBySeverity[severity];
              return (
                <li key={severity}>
                  <SeverityBadge severity={severity} />
                  <div className="bar-track" aria-hidden="true">
                    <div
                      className={`bar-fill severity-${severity.toLowerCase()}`}
                      style={{ width: `${(count / maxSeverity) * 100}%` }}
                    />
                  </div>
                  <span className="bar-count">{count}</span>
                </li>
              );
            })}
          </ul>
          <Link to="/findings?isResolved=false">View open findings →</Link>
        </div>

        <div className="panel">
          <h2>Upcoming inspections</h2>
          {data.upcomingInspections.length === 0 ? (
            <p className="muted">Nothing scheduled.</p>
          ) : (
            <ul className="simple-list">
              {data.upcomingInspections.map((inspection) => (
                <li key={inspection.id}>
                  <Link to={`/inspections/${inspection.id}`}>{inspection.title}</Link>
                  <span className="muted">
                    {inspection.siteName} · {formatDateTime(inspection.scheduledForUtc)} · {inspection.inspectorName}
                  </span>
                  <StatusBadge status={inspection.status} />
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </section>
  );
}
