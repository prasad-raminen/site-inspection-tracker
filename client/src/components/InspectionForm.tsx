import { useState, type FormEvent } from 'react';
import { STATUS_TRANSITIONS, type InspectionStatus, type Site } from '../api/types';
import { statusLabel } from '../utils/format';

export interface InspectionFormValues {
  siteId: string;
  title: string;
  inspectorName: string;
  scheduledFor: string; // datetime-local value
  status: InspectionStatus;
  notes: string;
}

interface InspectionFormProps {
  mode: 'create' | 'edit';
  sites: Pick<Site, 'id' | 'name'>[];
  initialValues: InspectionFormValues;
  /** Server-side field errors (camelCase keys) or a general `form` error. */
  serverErrors?: Record<string, string>;
  submitting?: boolean;
  onSubmit: (values: InspectionFormValues) => void;
  onCancel?: () => void;
}

export function validateInspection(values: InspectionFormValues, mode: 'create' | 'edit'): Record<string, string> {
  const errors: Record<string, string> = {};
  if (mode === 'create' && !values.siteId) errors.siteId = 'Choose a site.';
  if (!values.title.trim()) errors.title = 'Title is required.';
  else if (values.title.length > 200) errors.title = 'Title must be 200 characters or fewer.';
  if (!values.inspectorName.trim()) errors.inspectorName = 'Inspector name is required.';
  if (!values.scheduledFor || Number.isNaN(new Date(values.scheduledFor).getTime()))
    errors.scheduledForUtc = 'Choose a date and time.';
  return errors;
}

export function InspectionForm({
  mode,
  sites,
  initialValues,
  serverErrors = {},
  submitting = false,
  onSubmit,
  onCancel,
}: InspectionFormProps) {
  const [values, setValues] = useState(initialValues);
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({});
  const errors = { ...serverErrors, ...clientErrors };

  const statusOptions: InspectionStatus[] = [initialValues.status, ...STATUS_TRANSITIONS[initialValues.status]];

  const update = <K extends keyof InspectionFormValues>(key: K, value: InspectionFormValues[K]) =>
    setValues((current) => ({ ...current, [key]: value }));

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    const validation = validateInspection(values, mode);
    setClientErrors(validation);
    if (Object.keys(validation).length === 0) onSubmit(values);
  };

  return (
    <form className="panel form-grid" onSubmit={handleSubmit} noValidate aria-label="Inspection form">
      {errors.form && (
        <p className="alert" role="alert">
          {errors.form}
        </p>
      )}

      {mode === 'create' && (
        <label>
          Site
          <select value={values.siteId} onChange={(e) => update('siteId', e.target.value)}>
            <option value="">Select a site…</option>
            {sites.map((site) => (
              <option key={site.id} value={String(site.id)}>
                {site.name}
              </option>
            ))}
          </select>
          {errors.siteId && <span className="field-error">{errors.siteId}</span>}
        </label>
      )}

      <label>
        Title
        <input value={values.title} onChange={(e) => update('title', e.target.value)} maxLength={200} />
        {errors.title && <span className="field-error">{errors.title}</span>}
      </label>

      <label>
        Inspector
        <input value={values.inspectorName} onChange={(e) => update('inspectorName', e.target.value)} maxLength={120} />
        {errors.inspectorName && <span className="field-error">{errors.inspectorName}</span>}
      </label>

      <label>
        Scheduled for
        <input
          type="datetime-local"
          value={values.scheduledFor}
          onChange={(e) => update('scheduledFor', e.target.value)}
        />
        {errors.scheduledForUtc && <span className="field-error">{errors.scheduledForUtc}</span>}
      </label>

      {mode === 'edit' && (
        <label>
          Status
          <select value={values.status} onChange={(e) => update('status', e.target.value as InspectionStatus)}>
            {statusOptions.map((status) => (
              <option key={status} value={status}>
                {statusLabel(status)}
              </option>
            ))}
          </select>
          {errors.status && <span className="field-error">{errors.status}</span>}
        </label>
      )}

      <label className="full-width">
        Notes
        <textarea rows={4} value={values.notes} onChange={(e) => update('notes', e.target.value)} maxLength={4000} />
      </label>

      <div className="form-actions">
        {onCancel && (
          <button type="button" onClick={onCancel}>
            Cancel
          </button>
        )}
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Saving…' : mode === 'create' ? 'Schedule inspection' : 'Save changes'}
        </button>
      </div>
    </form>
  );
}
