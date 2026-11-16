// Mirrors the DTOs exposed by the ASP.NET Core API (enums are serialized as strings).

export const INSPECTION_STATUSES = ['Scheduled', 'InProgress', 'Completed', 'Failed'] as const;
export type InspectionStatus = (typeof INSPECTION_STATUSES)[number];

export const FINDING_SEVERITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
export type FindingSeverity = (typeof FINDING_SEVERITIES)[number];

/** Allowed next states, matching the domain state machine on the server. */
export const STATUS_TRANSITIONS: Record<InspectionStatus, InspectionStatus[]> = {
  Scheduled: ['InProgress'],
  InProgress: ['Completed', 'Failed'],
  Completed: [],
  Failed: ['Scheduled'],
};

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface Site {
  id: number;
  name: string;
  address: string;
  clientName: string | null;
  latitude: number | null;
  longitude: number | null;
  inspectionCount: number;
  openFindingCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface SaveSiteRequest {
  name: string;
  address: string;
  clientName?: string | null;
  latitude?: number | null;
  longitude?: number | null;
}

export interface Inspection {
  id: number;
  siteId: number;
  siteName: string;
  title: string;
  inspectorName: string;
  scheduledForUtc: string;
  completedAtUtc: string | null;
  status: InspectionStatus;
  notes: string | null;
  findingCount: number;
  openFindingCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface InspectionDetail {
  inspection: Inspection;
  findings: Finding[];
}

export interface CreateInspectionRequest {
  siteId: number;
  title: string;
  inspectorName: string;
  scheduledForUtc: string;
  notes?: string | null;
}

export interface UpdateInspectionRequest {
  title: string;
  inspectorName: string;
  scheduledForUtc: string;
  status: InspectionStatus;
  notes?: string | null;
}

export interface Finding {
  id: number;
  inspectionId: number;
  inspectionTitle: string;
  siteId: number;
  siteName: string;
  description: string;
  severity: FindingSeverity;
  isResolved: boolean;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
}

export interface CreateFindingRequest {
  description: string;
  severity: FindingSeverity;
}

export interface UpdateFindingRequest extends CreateFindingRequest {
  isResolved: boolean;
}

export interface DashboardSummary {
  totalSites: number;
  totalInspections: number;
  totalFindings: number;
  openFindings: number;
  inspectionsByStatus: Record<InspectionStatus, number>;
  openFindingsBySeverity: Record<FindingSeverity, number>;
  upcomingInspections: Inspection[];
}

/** RFC 7807 problem details as returned by the API for every error. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface PageQuery {
  page?: number;
  pageSize?: number;
}

export interface SiteQuery extends PageQuery {
  search?: string;
}

export interface InspectionQuery extends PageQuery {
  siteId?: number;
  status?: InspectionStatus;
  inspector?: string;
}

export interface FindingQuery extends PageQuery {
  siteId?: number;
  inspectionId?: number;
  severity?: FindingSeverity;
  minSeverity?: FindingSeverity;
  isResolved?: boolean;
}
