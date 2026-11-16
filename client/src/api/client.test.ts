import { describe, expect, it, vi } from 'vitest';
import { ApiError, buildQuery, createApiClient } from './client';

function jsonResponse(body: unknown, status = 200, contentType = 'application/json') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } });
}

describe('buildQuery', () => {
  it('skips undefined, null and empty values', () => {
    expect(buildQuery({ siteId: 3, status: undefined, inspector: '', isResolved: false, extra: null })).toBe(
      '?siteId=3&isResolved=false',
    );
  });

  it('returns an empty string when there is nothing to encode', () => {
    expect(buildQuery({})).toBe('');
  });
});

describe('createApiClient', () => {
  it('builds list URLs from the base URL and query', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ items: [], page: 1, pageSize: 10, totalCount: 0 }));
    const client = createApiClient('https://api.example.com/', fetchMock);

    await client.inspections.list({ siteId: 7, status: 'InProgress', page: 2 });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('https://api.example.com/api/inspections?siteId=7&status=InProgress&page=2');
    expect(new Headers(init.headers).get('Accept')).toBe('application/json');
  });

  it('sends JSON bodies with the correct method and content type', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ id: 1 }, 201));
    const client = createApiClient('', fetchMock);

    await client.inspections.addFinding(5, { description: 'Loose rail', severity: 'High' });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/api/inspections/5/findings');
    expect(init.method).toBe('POST');
    expect(new Headers(init.headers).get('Content-Type')).toBe('application/json');
    expect(JSON.parse(init.body as string)).toEqual({ description: 'Loose rail', severity: 'High' });
  });

  it('resolves to undefined for 204 No Content', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    const client = createApiClient('', fetchMock);

    await expect(client.sites.remove(1)).resolves.toBeUndefined();
  });

  it('throws ApiError exposing problem details and camelCased field errors', async () => {
    const problem = {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { Name: ['Name is required.'], ScheduledForUtc: ['Pick a date.', 'Must be UTC.'] },
    };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(problem, 400, 'application/problem+json'));
    const client = createApiClient('', fetchMock);

    const error = await client.sites.create({ name: '', address: 'x' }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    const apiError = error as ApiError;
    expect(apiError.status).toBe(400);
    expect(apiError.message).toBe('One or more validation errors occurred.');
    expect(apiError.fieldErrors).toEqual({ name: 'Name is required.', scheduledForUtc: 'Pick a date. Must be UTC.' });
  });

  it('still throws ApiError when the error body is not JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('Bad gateway', { status: 502 }));
    const client = createApiClient('', fetchMock);

    await expect(client.dashboard.summary()).rejects.toMatchObject({ status: 502, problem: undefined });
  });
});
