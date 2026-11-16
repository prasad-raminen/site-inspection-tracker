import { describe, expect, it } from 'vitest';
import { INSPECTION_STATUSES } from '../api/types';
import { formatDateTime, fromLocalInputValue, statusLabel, toLocalInputValue } from './format';

describe('format utilities', () => {
  it('formats missing or invalid timestamps as an em dash', () => {
    expect(formatDateTime(null)).toBe('—');
    expect(formatDateTime(undefined)).toBe('—');
    expect(formatDateTime('not-a-date')).toBe('—');
  });

  it('formats valid timestamps as a non-empty localized string', () => {
    const text = formatDateTime('2026-05-04T08:30:00Z');
    expect(text).not.toBe('—');
    expect(text).toMatch(/2026/);
  });

  it('has a human-readable label for every inspection status', () => {
    expect(statusLabel('InProgress')).toBe('In progress');
    for (const status of INSPECTION_STATUSES) {
      expect(statusLabel(status)).toBeTruthy();
    }
  });

  it('round-trips between datetime-local input values and UTC ISO strings', () => {
    const local = '2026-07-15T14:45';
    const iso = fromLocalInputValue(local);

    expect(iso).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:00\.000Z$/);
    expect(toLocalInputValue(iso)).toBe(local);
  });
});
