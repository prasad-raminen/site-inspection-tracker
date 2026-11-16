import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { FINDING_SEVERITIES } from '../api/types';
import { SeverityBadge } from './SeverityBadge';

describe('SeverityBadge', () => {
  it.each(FINDING_SEVERITIES)('renders %s with a matching style class and accessible title', (severity) => {
    render(<SeverityBadge severity={severity} />);

    const badge = screen.getByTitle(`${severity} severity`);
    expect(badge).toHaveTextContent(severity);
    expect(badge).toHaveClass(`severity-${severity.toLowerCase()}`);
  });
});
