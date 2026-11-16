import type { InspectionStatus } from '../api/types';

const dateTimeFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? '—' : dateTimeFormat.format(date);
}

const statusLabels: Record<InspectionStatus, string> = {
  Scheduled: 'Scheduled',
  InProgress: 'In progress',
  Completed: 'Completed',
  Failed: 'Failed',
};

export function statusLabel(status: InspectionStatus): string {
  return statusLabels[status];
}

/** Converts an ISO/UTC timestamp into the "yyyy-MM-ddTHH:mm" local value used by datetime-local inputs. */
export function toLocalInputValue(iso: string): string {
  const date = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(
    date.getMinutes(),
  )}`;
}

/** Converts a datetime-local value (interpreted in the user's time zone) to an ISO UTC string. */
export function fromLocalInputValue(value: string): string {
  return new Date(value).toISOString();
}
