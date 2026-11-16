import type {
  CreateFindingRequest,
  CreateInspectionRequest,
  DashboardSummary,
  Finding,
  FindingQuery,
  Inspection,
  InspectionDetail,
  InspectionQuery,
  PagedResult,
  ProblemDetails,
  SaveSiteRequest,
  Site,
  SiteQuery,
  UpdateFindingRequest,
  UpdateInspectionRequest,
} from './types';

/** Error thrown for any non-2xx response; carries the server's ProblemDetails when present. */
export class ApiError extends Error {
  readonly status: number;
  readonly problem?: ProblemDetails;

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  /** Field-level validation messages keyed by camelCase field name. */
  get fieldErrors(): Record<string, string> {
    const result: Record<string, string> = {};
    for (const [key, messages] of Object.entries(this.problem?.errors ?? {})) {
      const field = key.length > 0 ? key[0].toLowerCase() + key.slice(1) : key;
      result[field] = messages.join(' ');
    }
    return result;
  }
}

type QueryValue = string | number | boolean | null | undefined;

/** Builds "?a=1&b=x" from an object, skipping empty values. */
export function buildQuery(params: object = {}): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params as Record<string, QueryValue>)) {
    if (value === undefined || value === null || value === '') continue;
    search.append(key, String(value));
  }
  const query = search.toString();
  return query ? `?${query}` : '';
}

type FetchFn = typeof fetch;

export function createApiClient(baseUrl = '', fetchFn?: FetchFn) {
  const root = baseUrl.replace(/\/+$/, '');
  const doFetch: FetchFn = fetchFn ?? ((input, init) => globalThis.fetch(input, init));

  async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set('Accept', 'application/json');
    if (init.body !== undefined) headers.set('Content-Type', 'application/json');

    const response = await doFetch(`${root}${path}`, { ...init, headers });

    if (!response.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = (await response.json()) as ProblemDetails;
      } catch {
        problem = undefined;
      }
      throw new ApiError(response.status, problem);
    }

    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
  }

  const json = (body: unknown) => JSON.stringify(body);

  return {
    sites: {
      list: (query: SiteQuery = {}) => request<PagedResult<Site>>(`/api/sites${buildQuery(query)}`),
      get: (id: number) => request<Site>(`/api/sites/${id}`),
      create: (body: SaveSiteRequest) => request<Site>('/api/sites', { method: 'POST', body: json(body) }),
      update: (id: number, body: SaveSiteRequest) =>
        request<Site>(`/api/sites/${id}`, { method: 'PUT', body: json(body) }),
      remove: (id: number) => request<void>(`/api/sites/${id}`, { method: 'DELETE' }),
    },
    inspections: {
      list: (query: InspectionQuery = {}) =>
        request<PagedResult<Inspection>>(`/api/inspections${buildQuery(query)}`),
      get: (id: number) => request<InspectionDetail>(`/api/inspections/${id}`),
      create: (body: CreateInspectionRequest) =>
        request<Inspection>('/api/inspections', { method: 'POST', body: json(body) }),
      update: (id: number, body: UpdateInspectionRequest) =>
        request<Inspection>(`/api/inspections/${id}`, { method: 'PUT', body: json(body) }),
      remove: (id: number) => request<void>(`/api/inspections/${id}`, { method: 'DELETE' }),
      addFinding: (id: number, body: CreateFindingRequest) =>
        request<Finding>(`/api/inspections/${id}/findings`, { method: 'POST', body: json(body) }),
    },
    findings: {
      list: (query: FindingQuery = {}) => request<PagedResult<Finding>>(`/api/findings${buildQuery(query)}`),
      update: (id: number, body: UpdateFindingRequest) =>
        request<Finding>(`/api/findings/${id}`, { method: 'PUT', body: json(body) }),
      remove: (id: number) => request<void>(`/api/findings/${id}`, { method: 'DELETE' }),
    },
    dashboard: {
      summary: () => request<DashboardSummary>('/api/dashboard/summary'),
    },
  };
}

export type ApiClient = ReturnType<typeof createApiClient>;

/** Shared client; base URL comes from VITE_API_BASE_URL (empty = same origin / dev proxy). */
export const api = createApiClient(import.meta.env.VITE_API_BASE_URL ?? '');
