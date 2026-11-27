import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router";
import { ApiError } from "../../api/client";
import { requestsApi } from "../../api/endpoints";
import { BackLink } from "../../components/BackLink";
import { InsetText, ProblemPanel } from "../../components/Callouts";
import { Loading } from "../../components/Loading";
import { Page } from "../../components/Page";
import { StatusTag } from "../../components/StatusTag";
import { NotFoundPage } from "../../pages/NotFoundPage";
import { formatDateTime } from "../../lib/dates";
import { RequestDetails, RequestTimeline } from "./RequestDetails";

/** A requester's view of one of their own requests. */
export function RequestDetailPage() {
  const { id = "" } = useParams();
  const query = useQuery({ queryKey: ["requests", "detail", id], queryFn: () => requestsApi.get(id) });

  if (query.isLoading) return <Loading label="Loading the request" />;
  if (query.error instanceof ApiError && query.error.status === 404) return <NotFoundPage />;
  if (query.isError) return <ProblemPanel message={query.error.message} />;
  const request = query.data!;

  return (
    <Page title="Clinical space request" caption={request.reference} before={<BackLink to="/requests">Back to my requests</BackLink>}>
      <p className="mb-6 flex items-center gap-3">
        <span className="font-bold">Status:</span> <StatusTag status={request.status} />
      </p>

      {request.status === "Submitted" ? (
        <InsetText>
          <p className="m-0">An administrator will review this request. We will email you when a decision has been made.</p>
        </InsetText>
      ) : (
        <InsetText>
          <p className="m-0">
            {request.status === "Accepted" ? "Accepted" : "Rejected"} by {request.decidedByName} on{" "}
            {formatDateTime(request.decidedAt!)}.
          </p>
          {request.decisionNotes ? (
            <p className="mt-2 mb-0 whitespace-pre-line">
              <strong>{request.status === "Rejected" ? "Reason: " : "Notes: "}</strong>
              {request.decisionNotes}
            </p>
          ) : null}
        </InsetText>
      )}

      <RequestDetails request={request} />
      <RequestTimeline request={request} />
    </Page>
  );
}
