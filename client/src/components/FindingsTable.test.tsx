import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import type { Finding } from '../api/types';
import { FindingsTable } from './FindingsTable';

const findings: Finding[] = [
  {
    id: 1,
    inspectionId: 10,
    inspectionTitle: 'Level 12 slab pour',
    siteId: 3,
    siteName: 'Harbourfront Tower',
    description: 'Missing edge protection on east side',
    severity: 'Critical',
    isResolved: false,
    createdAtUtc: '2026-03-01T09:00:00Z',
    resolvedAtUtc: null,
  },
  {
    id: 2,
    inspectionId: 10,
    inspectionTitle: 'Level 12 slab pour',
    siteId: 3,
    siteName: 'Harbourfront Tower',
    description: 'Housekeeping around rebar stockpile',
    severity: 'Low',
    isResolved: true,
    createdAtUtc: '2026-03-01T09:30:00Z',
    resolvedAtUtc: '2026-03-02T10:00:00Z',
  },
];

function renderTable(props: Partial<Parameters<typeof FindingsTable>[0]> = {}) {
  return render(
    <MemoryRouter>
      <FindingsTable findings={findings} {...props} />
    </MemoryRouter>,
  );
}

describe('FindingsTable', () => {
  it('shows an empty state when there are no findings', () => {
    renderTable({ findings: [] });

    expect(screen.getByText('No findings.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('renders one row per finding with severity badge and status', () => {
    renderTable();

    const rows = screen.getAllByRole('row').slice(1); // skip header
    expect(rows).toHaveLength(2);
    expect(within(rows[0]).getByTitle('Critical severity')).toBeInTheDocument();
    expect(within(rows[0]).getByText('Open')).toBeInTheDocument();
    expect(within(rows[1]).getByText('Resolved')).toBeInTheDocument();
    expect(rows[1]).toHaveClass('resolved');
  });

  it('links to the site and inspection only when context columns are enabled', () => {
    const { unmount } = renderTable();
    expect(screen.queryByRole('link', { name: 'Harbourfront Tower' })).not.toBeInTheDocument();
    unmount();

    renderTable({ showContext: true });
    expect(screen.getAllByRole('link', { name: 'Harbourfront Tower' })[0]).toHaveAttribute('href', '/sites/3');
    expect(screen.getAllByRole('link', { name: 'Level 12 slab pour' })[0]).toHaveAttribute('href', '/inspections/10');
  });

  it('calls onToggleResolved with the clicked finding', async () => {
    const onToggleResolved = vi.fn();
    renderTable({ onToggleResolved });

    await userEvent.click(screen.getByRole('checkbox', { name: 'Mark "Missing edge protection on east side" as resolved' }));

    expect(onToggleResolved).toHaveBeenCalledTimes(1);
    expect(onToggleResolved).toHaveBeenCalledWith(findings[0]);
  });
});
