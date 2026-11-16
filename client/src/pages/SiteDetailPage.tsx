import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { INSPECTION_STATUSES, type InspectionStatus } from '../api/types';
import { ErrorMessage, Loading } from '../components/AsyncStatus';
import { Pagination } from '../components/Pagination';
import { StatusBadge } from '../components/StatusBadge';
import { useAsync } from '../hooks/useAsync';
import { formatDateTime, statusLabel } from '../utils/format';

export function SiteDetailPage() {
  const siteId = Number(useParams().siteId);
  const navigate = useNavigate();
  const [status, setStatus] = useState<InspectionStatus | ''>('');
  const [page, setPage] = useState(1);

  const site = useAsync(() => api.sites.get(siteId), [siteId]);
  const inspections = useAsync(
    () => api.inspections.list({ siteId, status: status || undefined, page, pageSize: 10 }),
    [siteId, status, page],
  );

  const onDelete = async () => {
    if (!site.data) return;
    if (!window.confirm(`Delete ${site.data.name} and all of its inspections?`)) return;
    await api.sites.remove(siteId);
    navigate('/sites');
  };

  if (site.loading && !site.data) return <Loading />;
  if (site.error) return <ErrorMessage error={site.error} onRetry={site.reload} />;
  if (!site.data) return null;

  const s = site.data;

  return (
    <section>
      <p className="breadcrumb">
        <Link to="/sites">Sites</Link> / {s.name}
      </p>
      <div className="page-header">
        <h1>{s.name}</h1>
        <div className="actions">
          <Link className="button primary" to={`/inspections/new?siteId=${s.id}`}>
            Schedule inspection
          </Link>
          <button type="button" className="danger" onClick={onDelete}>
            Delete site
          </button>
        </div>
      </div>

      <dl className="details">
        <dt>Address</dt>
        <dd>{s.address}</dd>
        <dt>Client</dt>
        <dd>{s.clientName ?? '—'}</dd>
        <dt>Coordinates</dt>
        <dd>{s.latitude !== null && s.longitude !== null ? `${s.latitude.toFixed(4)}, ${s.longitude.toFixed(4)}` : '—'}</dd>
        <dt>Open findings</dt>
        <dd>{s.openFindingCount}</dd>
      </dl>

      <div className="page-header">
        <h2>Inspections</h2>
        <label className="inline-label">
          Status
          <select
            value={status}
            onChange={(e) => {
              setPage(1);
              setStatus(e.target.value as InspectionStatus | '');
            }}
          >
            <option value="">All</option>
            {INSPECTION_STATUSES.map((value) => (
              <option key={value} value={value}>
                {statusLabel(value)}
              </option>
            ))}
          </select>
        </label>
      </div>

      {inspections.error && <ErrorMessage error={inspections.error} onRetry={inspections.reload} />}
      {inspections.loading && !inspections.data && <Loading />}
      {inspections.data && (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Title</th>
                <th>Inspector</th>
                <th>Scheduled</th>
                <th>Status</th>
                <th className="numeric">Open findings</th>
              </tr>
            </thead>
            <tbody>
              {inspections.data.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="muted">
                    No inspections yet.
                  </td>
                </tr>
              )}
              {inspections.data.items.map((inspection) => (
                <tr key={inspection.id}>
                  <td>
                    <Link to={`/inspections/${inspection.id}`}>{inspection.title}</Link>
                  </td>
                  <td>{inspection.inspectorName}</td>
                  <td>{formatDateTime(inspection.scheduledForUtc)}</td>
                  <td>
                    <StatusBadge status={inspection.status} />
                  </td>
                  <td className="numeric">{inspection.openFindingCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination
            page={inspections.data.page}
            totalPages={inspections.data.totalPages}
            totalCount={inspections.data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </section>
  );
}
