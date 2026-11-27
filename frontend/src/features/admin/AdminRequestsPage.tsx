import { useState, type FormEvent } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import { locationsApi, requestsApi } from "../../api/endpoints";
import type { RequestSortField, RequestStatus } from "../../api/types";
import { Button } from "../../components/Button";
import { InsetText, ProblemPanel } from "../../components/Callouts";
import { Loading } from "../../components/Loading";
import { Page } from "../../components/Page";
import { Pagination } from "../../components/Pagination";
import { RequestTable } from "../requests/RequestTable";

const SORTS: { value: string; label: string; sort: RequestSortField; descending: boolean }[] = [
  { value: "submitted-desc", label: "Newest submitted first", sort: "SubmittedAt", descending: true },
  { value: "submitted-asc", label: "Oldest submitted first", sort: "SubmittedAt", descending: false },
  { value: "start-asc", label: "Start date (soonest first)", sort: "StartsAt", descending: false },
  { value: "start-desc", label: "Start date (latest first)", sort: "StartsAt", descending: true },
  { value: "requester-asc", label: "Requester (A to Z)", sort: "Requester", descending: false },
  { value: "reference-desc", label: "Reference (highest first)", sort: "Reference", descending: true },
];

const STATUS_TABS: { status: RequestStatus | ""; label: string; key: "submitted" | "accepted" | "rejected" | "total" }[] = [
  { status: "Submitted", label: "To review", key: "submitted" },
  { status: "Accepted", label: "Accepted", key: "accepted" },
  { status: "Rejected", label: "Rejected", key: "rejected" },
  { status: "", label: "All", key: "total" },
];

/**
 * Administrator work queue. Opens on "To review" (Submitted), the requests that need action.
 * Filters live in the URL, so the back button, refresh and shared links keep the same view.
 */
export function AdminRequestsPage() {
  const [params, setParams] = useSearchParams();
  const status = (params.has("status") ? params.get("status") : "Submitted") as RequestStatus | "";
  const search = params.get("search") ?? "";
  const regionId = params.get("regionId") ?? "";
  const sortValue = params.get("sort") ?? (status === "Submitted" ? "submitted-asc" : "submitted-desc");
  const page = Math.max(1, Number(params.get("page") ?? 1) || 1);
  const sort = SORTS.find((s) => s.value === sortValue) ?? SORTS[0]!;

  const [searchText, setSearchText] = useState(search);
  const [regionText, setRegionText] = useState(regionId);
  const [sortText, setSortText] = useState(sortValue);

  const counts = useQuery({ queryKey: ["requests", "summary"], queryFn: requestsApi.summary });
  const regions = useQuery({ queryKey: ["locations", "regions"], queryFn: locationsApi.regions, staleTime: 5 * 60_000 });
  const list = useQuery({
    queryKey: ["requests", "all", { status, search, regionId, sortValue, page }],
    queryFn: () =>
      requestsApi.all({
        status: status || undefined,
        search: search || undefined,
        regionId: regionId ? Number(regionId) : undefined,
        sort: sort.sort,
        descending: sort.descending,
        page,
        pageSize: 15,
      }),
    placeholderData: keepPreviousData,
  });

  const update = (next: Record<string, string>) => {
    const merged = new URLSearchParams(params);
    for (const [key, value] of Object.entries(next)) merged.set(key, value);
    for (const [key, value] of [...merged.entries()]) if (value === "" && key !== "status") merged.delete(key);
    setParams(merged);
  };

  const applyFilters = (event: FormEvent) => {
    event.preventDefault();
    update({ search: searchText.trim(), regionId: regionText, sort: sortText, page: "" });
  };

  const clearFilters = () => {
    setSearchText("");
    setRegionText("");
    setSortText("submitted-desc");
    update({ search: "", regionId: "", sort: "", page: "" });
  };

  const hasFilters = Boolean(search || regionId);

  return (
    <Page title="Clinical space requests" caption="Administration">
      <nav aria-label="Filter by status" className="mb-8 grid grid-cols-2 gap-3 sm:grid-cols-4">
        {STATUS_TABS.map((tab) => {
          const active = tab.status === status;
          return (
            <button
              key={tab.key}
              type="button"
              aria-current={active ? "page" : undefined}
              onClick={() => {
                const nextSort = tab.status === "Submitted" ? "submitted-asc" : "submitted-desc";
                setSortText(nextSort);
                update({ status: tab.status, page: "", sort: nextSort });
              }}
              className={`border-2 bg-white p-4 text-left ${
                active ? "border-nhs-blue shadow-[inset_0_-6px_0_#005eb8]" : "border-nhs-pale-grey hover:border-nhs-mid-grey"
              }`}
            >
              <span className="block text-[2rem] leading-none font-bold" data-testid={`count-${tab.key}`}>
                {counts.data ? counts.data[tab.key] : "–"}
              </span>
              <span className={`mt-1 block ${active ? "font-bold" : "text-nhs-blue underline underline-offset-2"}`}>{tab.label}</span>
            </button>
          );
        })}
      </nav>

      <form onSubmit={applyFilters} className="nhs-card mb-6 grid gap-4 p-4 sm:grid-cols-[2fr_1.2fr_1.4fr_auto] sm:items-end sm:p-5" role="search" aria-label="Search and filter requests">
        <div>
          <label htmlFor="search" className="mb-1 block font-bold">
            Search
          </label>
          <input
            id="search"
            type="search"
            className="nhs-input"
            placeholder="Reference, name, cost code or building"
            value={searchText}
            maxLength={100}
            onChange={(e) => setSearchText(e.target.value)}
          />
        </div>
        <div>
          <label htmlFor="region" className="mb-1 block font-bold">
            Region
          </label>
          <select id="region" className="nhs-input" value={regionText} onChange={(e) => setRegionText(e.target.value)}>
            <option value="">All regions</option>
            {(regions.data ?? []).map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="sort" className="mb-1 block font-bold">
            Sort by
          </label>
          <select id="sort" className="nhs-input" value={sortText} onChange={(e) => setSortText(e.target.value)}>
            {SORTS.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
        <Button type="submit" variant="secondary" className="mb-0">
          Apply
        </Button>
      </form>

      {hasFilters ? (
        <p className="mb-4">
          <button type="button" className="text-nhs-blue underline underline-offset-2" onClick={clearFilters}>
            Clear search and filters
          </button>
        </p>
      ) : null}

      {list.isLoading ? <Loading label="Loading requests" /> : null}
      {list.isError ? <ProblemPanel message={list.error.message} /> : null}

      {list.data ? (
        <>
          <p className="nhs-body-s text-nhs-dark-grey" aria-live="polite" data-testid="result-count">
            {list.data.totalCount} request{list.data.totalCount === 1 ? "" : "s"}
            {status ? ` ${status === "Submitted" ? "waiting for review" : status.toLowerCase()}` : ""}
            {search ? ` matching “${search}”` : ""}
          </p>
          {list.data.items.length === 0 ? (
            <InsetText>
              <p className="m-0">
                {status === "Submitted" && !hasFilters ? "There are no requests waiting for review." : "No requests match your search and filters."}
              </p>
            </InsetText>
          ) : (
            <RequestTable caption="Clinical space requests" requests={list.data.items} linkBase="/admin/requests" showRequester />
          )}
          <Pagination page={list.data.page} totalPages={list.data.totalPages} onChange={(p) => update({ page: String(p) })} />
        </>
      ) : null}
    </Page>
  );
}
