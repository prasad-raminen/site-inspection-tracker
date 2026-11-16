import { Link } from 'react-router-dom';
import type { Finding } from '../api/types';
import { formatDateTime } from '../utils/format';
import { SeverityBadge } from './SeverityBadge';

interface FindingsTableProps {
  findings: Finding[];
  /** Show the site/inspection columns (used on the cross-site findings page). */
  showContext?: boolean;
  onToggleResolved?: (finding: Finding) => void;
}

export function FindingsTable({ findings, showContext = false, onToggleResolved }: FindingsTableProps) {
  if (findings.length === 0) {
    return <p className="muted">No findings.</p>;
  }

  return (
    <table className="data-table">
      <thead>
        <tr>
          <th>Severity</th>
          <th>Description</th>
          {showContext && <th>Site / inspection</th>}
          <th>Recorded</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {findings.map((finding) => (
          <tr key={finding.id} className={finding.isResolved ? 'resolved' : undefined}>
            <td>
              <SeverityBadge severity={finding.severity} />
            </td>
            <td>{finding.description}</td>
            {showContext && (
              <td>
                <Link to={`/sites/${finding.siteId}`}>{finding.siteName}</Link>
                <br />
                <Link className="muted" to={`/inspections/${finding.inspectionId}`}>
                  {finding.inspectionTitle}
                </Link>
              </td>
            )}
            <td>{formatDateTime(finding.createdAtUtc)}</td>
            <td>
              {onToggleResolved ? (
                <label className="inline-label">
                  <input
                    type="checkbox"
                    checked={finding.isResolved}
                    onChange={() => onToggleResolved(finding)}
                    aria-label={`Mark "${finding.description}" as ${finding.isResolved ? 'open' : 'resolved'}`}
                  />
                  {finding.isResolved ? 'Resolved' : 'Open'}
                </label>
              ) : finding.isResolved ? (
                'Resolved'
              ) : (
                'Open'
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
