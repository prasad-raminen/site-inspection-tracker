import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ApiError, api } from '../api/client';
import {
  FINDING_SEVERITIES,
  STATUS_TRANSITIONS,
  type Finding,
  type FindingSeverity,
  type Inspection,
  type InspectionStatus,
} from '../api/types';
import { ErrorMessage, Loading } from '../components/AsyncStatus';
import { FindingsTable } from '../components/FindingsTable';
import { StatusBadge } from '../components/StatusBadge';
import { useAsync } from '../hooks/useAsync';
import { formatDateTime } from '../utils/format';

const transitionLabels: Record<InspectionStatus, string> = {
  Scheduled: 'Reschedule',
  InProgress: 'Start inspection',
  Completed: 'Mark completed',
  Failed: 'Mark failed',
};

export function InspectionDetailPage() {
  const id = Number(useParams().inspectionId);
  const navigate = useNavigate();
  const { data, error, loading, reload } = useAsync(() => api.inspections.get(id), [id]);
  const [actionError, setActionError] = useState<string>();

  if (loading && !data) return <Loading />;
  if (error) return <ErrorMessage error={error} onRetry={reload} />;
  if (!data) return null;

  const { inspection, findings } = data;

  const changeStatus = async (status: InspectionStatus) => {
    setActionError(undefined);
    try {
      await api.inspections.update(id, {
        title: inspection.title,
        inspectorName: inspection.inspectorName,
        scheduledForUtc: inspection.scheduledForUtc,
        notes: inspection.notes,
        status,
      });
      reload();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Could not update status.');
    }
  };

  const toggleResolved = async (finding: Finding) => {
    setActionError(undefined);
    try {
      await api.findings.update(finding.id, {
        description: finding.description,
        severity: finding.severity,
        isResolved: !finding.isResolved,
      });
      reload();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Could not update finding.');
    }
  };

  const onDelete = async () => {
    if (!window.confirm('Delete this inspection and its findings?')) return;
    await api.inspections.remove(id);
    navigate(`/sites/${inspection.siteId}`);
  };

  return (
    <section>
      <p className="breadcrumb">
        <Link to="/sites">Sites</Link> / <Link to={`/sites/${inspection.siteId}`}>{inspection.siteName}</Link> /{' '}
        {inspection.title}
      </p>
      <div className="page-header">
        <h1>
          {inspection.title} <StatusBadge status={inspection.status} />
        </h1>
        <div className="actions">
          {STATUS_TRANSITIONS[inspection.status].map((next) => (
            <button key={next} type="button" onClick={() => changeStatus(next)}>
              {transitionLabels[next]}
            </button>
          ))}
          <Link className="button" to={`/inspections/${inspection.id}/edit`}>
            Edit
          </Link>
          <button type="button" className="danger" onClick={onDelete}>
            Delete
          </button>
        </div>
      </div>

      {actionError && (
        <p className="alert" role="alert">
          {actionError}
        </p>
      )}

      <dl className="details">
        <dt>Inspector</dt>
        <dd>{inspection.inspectorName}</dd>
        <dt>Scheduled for</dt>
        <dd>{formatDateTime(inspection.scheduledForUtc)}</dd>
        <dt>Finished</dt>
        <dd>{formatDateTime(inspection.completedAtUtc)}</dd>
        <dt>Notes</dt>
        <dd>{inspection.notes ?? '—'}</dd>
      </dl>

      <h2>
        Findings <span className="muted">({inspection.openFindingCount} open)</span>
      </h2>
      <FindingsTable findings={findings} onToggleResolved={toggleResolved} />

      <AddFindingForm inspection={inspection} onAdded={reload} />
    </section>
  );
}

function AddFindingForm({ inspection, onAdded }: { inspection: Inspection; onAdded: () => void }) {
  const [description, setDescription] = useState('');
  const [severity, setSeverity] = useState<FindingSeverity>('Medium');
  const [error, setError] = useState<string>();
  const [submitting, setSubmitting] = useState(false);

  if (inspection.status === 'Scheduled') {
    return <p className="muted">Start the inspection to record findings.</p>;
  }

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!description.trim()) {
      setError('Describe the finding.');
      return;
    }
    setSubmitting(true);
    setError(undefined);
    try {
      await api.inspections.addFinding(inspection.id, { description: description.trim(), severity });
      setDescription('');
      onAdded();
    } catch (err) {
      setError(err instanceof ApiError ? (err.fieldErrors.description ?? err.message) : 'Could not add finding.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form className="panel form-grid" onSubmit={onSubmit} noValidate>
      <h3 className="full-width">Record a finding</h3>
      <label className="full-width">
        Description
        <textarea rows={2} value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} />
        {error && <span className="field-error">{error}</span>}
      </label>
      <label>
        Severity
        <select value={severity} onChange={(e) => setSeverity(e.target.value as FindingSeverity)}>
          {FINDING_SEVERITIES.map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </select>
      </label>
      <div className="form-actions">
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Adding…' : 'Add finding'}
        </button>
      </div>
    </form>
  );
}
