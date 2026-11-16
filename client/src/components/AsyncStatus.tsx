import { ApiError } from '../api/client';

export function Loading({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="muted" role="status">
      {label}
    </p>
  );
}

export function ErrorMessage({ error, onRetry }: { error: Error; onRetry?: () => void }) {
  const message =
    error instanceof ApiError && error.status === 404
      ? 'Not found.'
      : error instanceof TypeError
        ? 'Could not reach the API. Is the backend running?'
        : error.message;

  return (
    <div className="alert" role="alert">
      <span>{message}</span>
      {onRetry && (
        <button type="button" className="link-button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}
