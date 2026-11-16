import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { ApiError, api } from '../api/client';
import { ErrorMessage, Loading } from '../components/AsyncStatus';
import { InspectionForm, type InspectionFormValues } from '../components/InspectionForm';
import { useAsync } from '../hooks/useAsync';
import { fromLocalInputValue, toLocalInputValue } from '../utils/format';

/** Create (/inspections/new?siteId=1) and edit (/inspections/:inspectionId/edit) page. */
export function InspectionFormPage() {
  const { inspectionId } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const mode = inspectionId ? 'edit' : 'create';
  const id = Number(inspectionId);

  const [serverErrors, setServerErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

  const loaded = useAsync(async () => {
    if (mode === 'edit') {
      const detail = await api.inspections.get(id);
      return { sites: [], inspection: detail.inspection };
    }
    const sites = await api.sites.list({ pageSize: 100 });
    return { sites: sites.items, inspection: undefined };
  }, [mode, id]);

  if (loaded.loading && !loaded.data) return <Loading />;
  if (loaded.error) return <ErrorMessage error={loaded.error} onRetry={loaded.reload} />;
  if (!loaded.data) return null;

  const { sites, inspection } = loaded.data;
  const defaultDate = new Date(Date.now() + 24 * 60 * 60 * 1000);
  defaultDate.setMinutes(0, 0, 0);

  const initialValues: InspectionFormValues = inspection
    ? {
        siteId: String(inspection.siteId),
        title: inspection.title,
        inspectorName: inspection.inspectorName,
        scheduledFor: toLocalInputValue(inspection.scheduledForUtc),
        status: inspection.status,
        notes: inspection.notes ?? '',
      }
    : {
        siteId: searchParams.get('siteId') ?? '',
        title: '',
        inspectorName: '',
        scheduledFor: toLocalInputValue(defaultDate.toISOString()),
        status: 'Scheduled',
        notes: '',
      };

  const onSubmit = async (values: InspectionFormValues) => {
    setSubmitting(true);
    setServerErrors({});
    try {
      const common = {
        title: values.title.trim(),
        inspectorName: values.inspectorName.trim(),
        scheduledForUtc: fromLocalInputValue(values.scheduledFor),
        notes: values.notes.trim() || null,
      };
      const saved =
        mode === 'edit'
          ? await api.inspections.update(id, { ...common, status: values.status })
          : await api.inspections.create({ ...common, siteId: Number(values.siteId) });
      navigate(`/inspections/${saved.id}`);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) setServerErrors(err.fieldErrors);
      else setServerErrors({ form: err instanceof Error ? err.message : 'Could not save the inspection.' });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section>
      <p className="breadcrumb">
        {inspection ? (
          <>
            <Link to={`/sites/${inspection.siteId}`}>{inspection.siteName}</Link> /{' '}
            <Link to={`/inspections/${inspection.id}`}>{inspection.title}</Link> / Edit
          </>
        ) : (
          <>
            <Link to="/sites">Sites</Link> / New inspection
          </>
        )}
      </p>
      <h1>{mode === 'edit' ? 'Edit inspection' : 'Schedule inspection'}</h1>
      <InspectionForm
        mode={mode}
        sites={sites}
        initialValues={initialValues}
        serverErrors={serverErrors}
        submitting={submitting}
        onSubmit={onSubmit}
        onCancel={() => navigate(-1)}
      />
    </section>
  );
}
