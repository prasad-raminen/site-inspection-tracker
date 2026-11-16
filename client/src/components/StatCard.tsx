interface StatCardProps {
  label: string;
  value: number;
  tone?: 'default' | 'warning' | 'danger' | 'success' | 'info';
  hint?: string;
}

export function StatCard({ label, value, tone = 'default', hint }: StatCardProps) {
  return (
    <div className={`stat-card tone-${tone}`}>
      <span className="stat-label">{label}</span>
      <span className="stat-value">{value}</span>
      {hint && <span className="stat-hint">{hint}</span>}
    </div>
  );
}
