import type { InspectionStatus } from '../api/types';
import { statusLabel } from '../utils/format';

export function StatusBadge({ status }: { status: InspectionStatus }) {
  return <span className={`badge status-${status.toLowerCase()}`}>{statusLabel(status)}</span>;
}
