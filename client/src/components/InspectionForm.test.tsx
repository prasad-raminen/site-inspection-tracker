import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { InspectionForm, type InspectionFormValues } from './InspectionForm';

const sites = [
  { id: 1, name: 'Harbourfront Tower' },
  { id: 2, name: 'Northgate Rail Bridge' },
];

const blank: InspectionFormValues = {
  siteId: '',
  title: '',
  inspectorName: '',
  scheduledFor: '2026-06-01T09:00',
  status: 'Scheduled',
  notes: '',
};

describe('InspectionForm', () => {
  it('shows validation errors and does not submit when required fields are missing', async () => {
    const onSubmit = vi.fn();
    render(<InspectionForm mode="create" sites={sites} initialValues={blank} onSubmit={onSubmit} />);

    await userEvent.click(screen.getByRole('button', { name: 'Schedule inspection' }));

    expect(screen.getByText('Choose a site.')).toBeInTheDocument();
    expect(screen.getByText('Title is required.')).toBeInTheDocument();
    expect(screen.getByText('Inspector name is required.')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('submits the entered values when valid', async () => {
    const onSubmit = vi.fn();
    render(<InspectionForm mode="create" sites={sites} initialValues={blank} onSubmit={onSubmit} />);

    await userEvent.selectOptions(screen.getByLabelText('Site'), '2');
    await userEvent.type(screen.getByLabelText('Title'), 'Deck crack mapping');
    await userEvent.type(screen.getByLabelText('Inspector'), 'Priya Raman');
    await userEvent.click(screen.getByRole('button', { name: 'Schedule inspection' }));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ siteId: '2', title: 'Deck crack mapping', inspectorName: 'Priya Raman' }),
    );
  });

  it('only offers legal status transitions when editing', () => {
    render(
      <InspectionForm
        mode="edit"
        sites={[]}
        initialValues={{ ...blank, siteId: '1', title: 'Check', inspectorName: 'Sam', status: 'InProgress' }}
        onSubmit={vi.fn()}
      />,
    );

    const options = within(screen.getByLabelText('Status'))
      .getAllByRole('option')
      .map((o) => o.textContent);
    expect(options).toEqual(['In progress', 'Completed', 'Failed']);
    expect(screen.queryByLabelText('Site')).not.toBeInTheDocument();
  });

  it('displays server-side field errors', () => {
    render(
      <InspectionForm
        mode="create"
        sites={sites}
        initialValues={blank}
        serverErrors={{ siteId: 'Site 99 does not exist.' }}
        onSubmit={vi.fn()}
      />,
    );

    expect(screen.getByText('Site 99 does not exist.')).toBeInTheDocument();
  });
});
