import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import { requestsApi } from "../../api/endpoints";
import type { RequestStatus } from "../../api/types";
import { ButtonLink } from "../../components/Button";
import { InsetText, ProblemPanel } from "../../components/Callouts";
import { Loading } from "../../components/Loading";
import { Page } from "../../components/Page";
import { Pagination } from "../../components/Pagination";
import { RequestTable } from "./RequestTable";

const STATUSES: (RequestStatus | "")[] = ["", "Submitted", "Accepted", "Rejected"];

/** "My Requests": everything the signed-in requester has submitted, newest first. */
export function MyRequestsPage() {
  const [params, setParams] = useSearchParams();
  const status = (params.get("status") ?? "") as RequestStatus | "";
  const page = Math.max(1, Number(params.get("page") ?? 1) || 1);

  const query = useQuery({
    queryKey: ["requests", "mine", { status, page }],
    queryFn: () => requestsApi.mine({ status: status || undefined, page, pageSize: 10 }),
    placeholderData: keepPreviousData,
  });

  const update = (next: Record<string, string>) => {
    const merged = new URLSearchParams(params);
    for (const [key, value] of Object.entries(next)) {
      if (value) merged.set(key, value);
      else merged.delete(key);
    }
    setParams(merged);
  };

  return (
    <Page title="My requests" headingAside={<ButtonLink to="/requests/new">Request a clinical space</ButtonLink>}>
      <nav aria-label="Filter by status" className="mb-6 border-b border-nhs-pale-grey">
        <ul className="-mb-px flex flex-wrap gap-x-1">
          {STATUSES.map((s) => {
            const active = s === status;
            return (
              <li key={s || "all"}>
                <button
                  type="button"
                  aria-current={active ? "page" : undefined}
                  onClick={() => update({ status: s, page: "" })}
                  className={`border-x border-t px-4 py-2 ${
                    active
                      ? "border-nhs-pale-grey bg-white font-bold text-nhs-black"
                      : "border-transparent text-nhs-blue underline underline-offset-2 hover:text-nhs-link-hover"
                  }`}
                >
                  {s || "All"}
                </button>
              </li>
            );
          })}
        </ul>
      </nav>

      {query.isLoading ? <Loading label="Loading your requests" /> : null}
      {query.isError ? <ProblemPanel message={query.error.message} /> : null}

      {query.data && query.data.items.length === 0 ? (
        <InsetText>
          <p className="m-0">
            {status ? `You have no ${status.toLowerCase()} requests.` : "You have not made any requests yet."}
          </p>
        </InsetText>
      ) : null}

      {query.data && query.data.items.length > 0 ? (
        <>
          <p className="nhs-body-s text-nhs-dark-grey" aria-live="polite">
            Showing {query.data.items.length} of {query.data.totalCount} request{query.data.totalCount === 1 ? "" : "s"}
          </p>
          <RequestTable caption="Your clinical space requests" requests={query.data.items} linkBase="/requests" />
          <Pagination page={query.data.page} totalPages={query.data.totalPages} onChange={(p) => update({ page: String(p) })} />
        </>
      ) : null}
    </Page>
  );
}
