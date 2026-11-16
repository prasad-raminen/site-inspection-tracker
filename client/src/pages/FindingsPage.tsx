import { useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { FINDING_SEVERITIES, type Finding, type FindingSeverity } from '../api/types';
import { ErrorMessage, Loading } from '../components/AsyncStatus';
import { FindingsTable } from '../components/FindingsTable';
import { Pagination } from '../components/Pagination';
import { useAsync } from '../hooks/useAsync';

/** Cross-site findings list; filters live in the URL so views can be bookmarked/shared. */
export function FindingsPage() {
  const [params, setParams] = useSearchParams();
  const severity = (params.get('severity') ?? '') as FindingSeverity | '';
  const resolved = params.get('isResolved') ?? '';
  const page = Number(params.get('page') ?? '1') || 1;

  const { data, error, loading, reload } = useAsync(
    () =>
      api.findings.list({
        severity: severity || undefined,
        isResolved: resolved === '' ? undefined : resolved === 'true',
        page,
        pageSize: 20,
      }),
    [severity, resolved, page],
  );

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== 'page') next.delete('page');
    setParams(next);
  };

  const toggleResolved = async (finding: Finding) => {
    await api.findings.update(finding.id, {
      description: finding.description,
      severity: finding.severity,
      isResolved: !finding.isResolved,
    });
    reload();
  };

  return (
    <section>
      <h1>Findings</h1>
      <div className="toolbar">
        <label className="inline-label">
          Severity
          <select value={severity} onChange={(e) => setFilter('severity', e.target.value)}>
            <option value="">All</option>
            {FINDING_SEVERITIES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>
        <label className="inline-label">
          State
          <select value={resolved} onChange={(e) => setFilter('isResolved', e.target.value)}>
            <option value="">All</option>
            <option value="false">Open</option>
            <option value="true">Resolved</option>
          </select>
        </label>
      </div>

      {error && <ErrorMessage error={error} onRetry={reload} />}
      {loading && !data && <Loading />}
      {data && (
        <>
          <FindingsTable findings={data.items} showContext onToggleResolved={toggleResolved} />
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={(p) => setFilter('page', String(p))}
          />
        </>
      )}
    </section>
  );
}
