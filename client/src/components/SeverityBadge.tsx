import type { FindingSeverity } from '../api/types';

const icons: Record<FindingSeverity, string> = {
  Low: '●',
  Medium: '▲',
  High: '◆',
  Critical: '■',
};

/** Severity pill; colour plus a distinct glyph so it does not rely on colour alone. */
export function SeverityBadge({ severity }: { severity: FindingSeverity }) {
  return (
    <span className={`badge severity-${severity.toLowerCase()}`} title={`${severity} severity`}>
      <span aria-hidden="true">{icons[severity]}</span> {severity}
    </span>
  );
}
