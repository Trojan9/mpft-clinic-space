import { apiFetch, toQueryString } from "./client";
import type {
  CreateRequestPayload,
  DecisionPayload,
  LocationOption,
  PagedResult,
  RequestDetail,
  RequestListQuery,
  RequestSummary,
  StatusCounts,
  User,
} from "./types";

export const authApi = {
  me: () => apiFetch<User>("/api/auth/me"),
  login: (username: string, password: string) =>
    apiFetch<User>("/api/auth/login", { method: "POST", body: JSON.stringify({ username, password }) }),
  logout: () => apiFetch<void>("/api/auth/logout", { method: "POST" }),
};

export const locationsApi = {
  regions: () => apiFetch<LocationOption[]>("/api/locations/regions"),
  sites: (regionId: number) => apiFetch<LocationOption[]>(`/api/locations/regions/${regionId}/sites`),
  buildings: (siteId: number) => apiFetch<LocationOption[]>(`/api/locations/sites/${siteId}/buildings`),
};

export const requestsApi = {
  create: (payload: CreateRequestPayload) =>
    apiFetch<RequestDetail>("/api/requests", { method: "POST", body: JSON.stringify(payload) }),
  mine: (query: RequestListQuery) => apiFetch<PagedResult<RequestSummary>>(`/api/requests/mine${toQueryString(query)}`),
  all: (query: RequestListQuery) => apiFetch<PagedResult<RequestSummary>>(`/api/requests${toQueryString(query)}`),
  summary: () => apiFetch<StatusCounts>("/api/requests/summary"),
  get: (id: string) => apiFetch<RequestDetail>(`/api/requests/${encodeURIComponent(id)}`),
  decide: (id: string, payload: DecisionPayload) =>
    apiFetch<RequestDetail>(`/api/requests/${encodeURIComponent(id)}/decision`, {
      method: "POST",
      body: JSON.stringify(payload),
    }),
};
