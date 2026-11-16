import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { ApiError, api } from '../api/client';
import { ErrorMessage, Loading } from '../components/AsyncStatus';
import { Pagination } from '../components/Pagination';
import { useAsync } from '../hooks/useAsync';

const PAGE_SIZE = 10;

export function SitesPage() {
  const [search, setSearch] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);

  const { data, error, loading, reload } = useAsync(
    () => api.sites.list({ search: appliedSearch, page, pageSize: PAGE_SIZE }),
    [appliedSearch, page],
  );

  const onSearch = (event: FormEvent) => {
    event.preventDefault();
    setPage(1);
    setAppliedSearch(search.trim());
  };

  return (
    <section>
      <div className="page-header">
        <h1>Sites</h1>
        <button type="button" className="primary" onClick={() => setShowForm((v) => !v)}>
          {showForm ? 'Cancel' : 'New site'}
        </button>
      </div>

      {showForm && (
        <NewSiteForm
          onCreated={() => {
            setShowForm(false);
            reload();
          }}
        />
      )}

      <form className="toolbar" onSubmit={onSearch} role="search">
        <input
          type="search"
          placeholder="Search by name, address or client"
          aria-label="Search sites"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button type="submit">Search</button>
      </form>

      {error && <ErrorMessage error={error} onRetry={reload} />}
      {loading && !data && <Loading />}

      {data && (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Address</th>
                <th>Client</th>
                <th className="numeric">Inspections</th>
                <th className="numeric">Open findings</th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="muted">
                    No sites found.
                  </td>
                </tr>
              )}
              {data.items.map((site) => (
                <tr key={site.id}>
                  <td>
                    <Link to={`/sites/${site.id}`}>{site.name}</Link>
                  </td>
                  <td>{site.address}</td>
                  <td>{site.clientName ?? '—'}</td>
                  <td className="numeric">{site.inspectionCount}</td>
                  <td className="numeric">
                    {site.openFindingCount > 0 ? <strong>{site.openFindingCount}</strong> : 0}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  );
}

function NewSiteForm({ onCreated }: { onCreated: () => void }) {
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [clientName, setClientName] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setErrors({});
    try {
      await api.sites.create({ name, address, clientName: clientName || null });
      onCreated();
    } catch (err) {
      if (err instanceof ApiError) {
        setErrors(Object.keys(err.fieldErrors).length ? err.fieldErrors : { form: err.message });
      } else {
        setErrors({ form: 'Could not create the site.' });
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form className="panel form-grid" onSubmit={onSubmit} noValidate>
      <h2>New site</h2>
      {errors.form && <p className="alert">{errors.form}</p>}
      <label>
        Name
        <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={200} />
        {errors.name && <span className="field-error">{errors.name}</span>}
      </label>
      <label>
        Address
        <input value={address} onChange={(e) => setAddress(e.target.value)} required maxLength={500} />
        {errors.address && <span className="field-error">{errors.address}</span>}
      </label>
      <label>
        Client (optional)
        <input value={clientName} onChange={(e) => setClientName(e.target.value)} maxLength={200} />
      </label>
      <div className="form-actions">
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Saving…' : 'Create site'}
        </button>
      </div>
    </form>
  );
}
