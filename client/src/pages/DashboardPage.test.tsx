import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import type { DashboardSummary } from '../api/types';
import { DashboardPage } from './DashboardPage';

const summary: DashboardSummary = {
  totalSites: 4,
  totalInspections: 8,
  totalFindings: 10,
  openFindings: 6,
  inspectionsByStatus: { Scheduled: 4, InProgress: 1, Completed: 2, Failed: 1 },
  openFindingsBySeverity: { Low: 1, Medium: 2, High: 1, Critical: 2 },
  upcomingInspections: [
    {
      id: 11,
      siteId: 1,
      siteName: 'Harbourfront Tower',
      title: 'Weekly scaffold safety walk',
      inspectorName: 'Aroha Ngata',
      scheduledForUtc: '2026-06-01T08:00:00Z',
      completedAtUtc: null,
      status: 'Scheduled',
      notes: null,
      findingCount: 0,
      openFindingCount: 0,
      createdAtUtc: '2026-05-01T08:00:00Z',
      updatedAtUtc: null,
    },
  ],
};

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('renders summary cards and upcoming inspections from the API', async () => {
    vi.spyOn(api.dashboard, 'summary').mockResolvedValue(summary);

    render(
      <MemoryRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <DashboardPage />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument();
    expect(screen.getByText('Critical open').nextSibling).toHaveTextContent('2');
    expect(screen.getByText('Open findings').nextSibling).toHaveTextContent('6');
    expect(screen.getByText('In progress').nextSibling).toHaveTextContent('1');
    expect(screen.getByRole('link', { name: 'Weekly scaffold safety walk' })).toHaveAttribute('href', '/inspections/11');
  });

  it('shows a friendly message when the API cannot be reached', async () => {
    vi.spyOn(api.dashboard, 'summary').mockRejectedValue(new TypeError('Failed to fetch'));

    render(
      <MemoryRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <DashboardPage />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not reach the API');
  });
});
