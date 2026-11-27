// Types that mirror the API contracts (see the OpenAPI document at /api/docs in development).

export type UserRole = "Requester" | "Administrator";
export type RequestStatus = "Submitted" | "Accepted" | "Rejected";
export type RequestEventType = "Submitted" | "Accepted" | "Rejected";
export type Decision = "Accept" | "Reject";
export type RequestSortField = "SubmittedAt" | "StartsAt" | "Reference" | "Requester" | "Status";

export interface User {
  id: string;
  username: string;
  displayName: string;
  email: string;
  role: UserRole;
}

export interface LocationOption {
  id: number;
  name: string;
}

export interface LocationPath {
  regionId: number;
  region: string;
  siteId: number;
  site: string;
  buildingId: number;
  building: string;
}

export interface RequestSummary {
  id: string;
  reference: string;
  status: RequestStatus;
  requesterName: string;
  region: string;
  site: string;
  building: string;
  startsAt: string;
  endsAt: string;
  submittedAt: string;
  decidedAt: string | null;
}

export interface RequestEvent {
  type: RequestEventType;
  actorName: string;
  occurredAt: string;
  notes: string | null;
}

export interface OverlappingRequest {
  id: string;
  reference: string;
  status: RequestStatus;
  requesterName: string;
  startsAt: string;
  endsAt: string;
}

export interface RequestDetail {
  id: string;
  reference: string;
  status: RequestStatus;
  requesterName: string;
  requesterEmail: string;
  costCode: string;
  budgetHolderName: string;
  budgetHolderEmail: string;
  location: LocationPath;
  startsAt: string;
  endsAt: string;
  sinkRequired: boolean;
  clinicalCouchRequired: boolean;
  additionalRequirements: string | null;
  termsVersion: string;
  termsAcceptedAt: string;
  submittedAt: string;
  updatedAt: string;
  decidedByName: string | null;
  decidedAt: string | null;
  decisionNotes: string | null;
  version: string;
  timeline: RequestEvent[];
  overlappingRequests: OverlappingRequest[] | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface StatusCounts {
  submitted: number;
  accepted: number;
  rejected: number;
  total: number;
}

export interface CreateRequestPayload {
  costCode: string;
  budgetHolderName: string;
  budgetHolderEmail: string;
  buildingId: number;
  startsAt: string;
  endsAt: string;
  sinkRequired: boolean;
  clinicalCouchRequired: boolean;
  additionalRequirements: string | null;
  acceptTerms: boolean;
}

export interface DecisionPayload {
  decision: Decision;
  notes: string | null;
  version: string;
}

export interface RequestListQuery {
  status?: RequestStatus;
  regionId?: number;
  search?: string;
  sort?: RequestSortField;
  descending?: boolean;
  page?: number;
  pageSize?: number;
}
