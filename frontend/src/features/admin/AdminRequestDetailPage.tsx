import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { ApiError } from "../../api/client";
import { requestsApi } from "../../api/endpoints";
import type { RequestDetail } from "../../api/types";
import { BackLink } from "../../components/BackLink";
import { Button } from "../../components/Button";
import { InsetText, ProblemPanel, SuccessBanner, WarningCallout } from "../../components/Callouts";
import { ErrorSummary, type ErrorSummaryItem } from "../../components/ErrorSummary";
import { Radios, Textarea } from "../../components/FormFields";
import { Loading } from "../../components/Loading";
import { Page } from "../../components/Page";
import { StatusTag } from "../../components/StatusTag";
import { formatDateTime, formatShortPeriod } from "../../lib/dates";
import { NotFoundPage } from "../../pages/NotFoundPage";
import { RequestDetails, RequestTimeline } from "../requests/RequestDetails";

const NOTES_MAX = 1000;

const decisionSchema = z
  .object({
    decision: z.enum(["Accept", "Reject"], { error: "Select whether to accept or reject the request" }),
    notes: z.string().max(NOTES_MAX, `Notes must be ${NOTES_MAX} characters or fewer`),
  })
  .superRefine((value, ctx) => {
    if (value.decision === "Reject" && !value.notes.trim()) {
      ctx.addIssue({ code: "custom", path: ["notes"], message: "Enter a reason for rejecting the request" });
    }
  });
type DecisionForm = z.infer<typeof decisionSchema>;

function DecisionPanel({ request, onDecided }: { request: RequestDetail; onDecided: (updated: RequestDetail) => void }) {
  const [serverError, setServerError] = useState<{ message: string; conflict: boolean } | null>(null);
  const queryClient = useQueryClient();
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, submitCount },
  } = useForm<DecisionForm>({
    resolver: zodResolver(decisionSchema),
    defaultValues: { decision: undefined, notes: "" },
    shouldFocusError: false,
  });
  const decision = watch("decision");
  const notes = watch("notes") ?? "";

  const mutation = useMutation({
    mutationFn: (values: DecisionForm) =>
      requestsApi.decide(request.id, { decision: values.decision, notes: values.notes.trim() || null, version: request.version }),
    onSuccess: async (updated) => {
      queryClient.setQueryData(["requests", "detail", request.id], updated);
      await queryClient.invalidateQueries({ queryKey: ["requests"], refetchType: "active" });
      onDecided(updated);
    },
    onError: (error) => {
      const conflict = error instanceof ApiError && error.status === 409;
      setServerError({
        message: error instanceof ApiError ? error.message : "Sorry, there is a problem with the service. Try again later.",
        conflict,
      });
    },
  });

  const summary: ErrorSummaryItem[] = [
    ...(errors.decision ? [{ fieldId: "decision", message: errors.decision.message! }] : []),
    ...(errors.notes ? [{ fieldId: "notes", message: errors.notes.message! }] : []),
    ...(serverError ? [{ message: serverError.message }] : []),
  ];

  return (
    <section aria-labelledby="decision-heading" className="nhs-card border-t-8 border-t-nhs-blue">
      <h2 id="decision-heading" className="nhs-heading-l">
        Make a decision
      </h2>
      {(submitCount > 0 || serverError) && summary.length > 0 ? <ErrorSummary errors={summary} /> : null}
      {serverError?.conflict ? (
        <p>
          <button
            type="button"
            className="text-nhs-blue underline underline-offset-2"
            onClick={() => {
              setServerError(null);
              void queryClient.invalidateQueries({ queryKey: ["requests", "detail", request.id] });
            }}
          >
            Reload the latest version of this request
          </button>
        </p>
      ) : null}
      <form noValidate onSubmit={handleSubmit((values) => mutation.mutate(values))} aria-label="Make a decision">
        <Radios
          id="decision"
          legend="Do you want to accept or reject this request?"
          options={[
            { value: "Accept", label: "Accept", hint: "The requester can use the space at the time they asked for." },
            { value: "Reject", label: "Reject", hint: "You must give a reason. The requester will see it." },
          ]}
          error={errors.decision?.message}
          inputProps={register("decision")}
        />
        <Textarea
          id="notes"
          label={decision === "Reject" ? "Reason for rejecting" : "Notes for the requester (optional)"}
          hint={
            decision === "Reject"
              ? "Explain why, and what they can do instead. This is included in the email."
              : "For example, which room to use or where to collect a key. This is included in the email."
          }
          maxChars={NOTES_MAX}
          currentLength={notes.length}
          error={errors.notes?.message}
          {...register("notes")}
        />
        <p className="nhs-body-s text-nhs-dark-grey">
          We will email {request.requesterName} at {request.requesterEmail} with your decision.
        </p>
        <Button type="submit" variant={decision === "Reject" ? "warning" : "primary"} disabled={mutation.isPending} className="mb-0">
          {mutation.isPending ? "Saving…" : decision === "Reject" ? "Reject request" : decision === "Accept" ? "Accept request" : "Save decision"}
        </Button>
      </form>
    </section>
  );
}

/** Administrator review page: full details, clashes with other bookings, and the decision form. */
export function AdminRequestDetailPage() {
  const { id = "" } = useParams();
  const [decided, setDecided] = useState<RequestDetail | null>(null);
  const query = useQuery({ queryKey: ["requests", "detail", id], queryFn: () => requestsApi.get(id) });

  if (query.isLoading) return <Loading label="Loading the request" />;
  if (query.error instanceof ApiError && query.error.status === 404) return <NotFoundPage />;
  if (query.isError) return <ProblemPanel message={query.error.message} />;
  const request = query.data!;
  const overlaps = request.overlappingRequests ?? [];

  return (
    <Page
      title="Review request"
      caption={request.reference}
      before={
        <>
          <BackLink to="/admin/requests">Back to all requests</BackLink>
          {decided ? (
            <SuccessBanner title={`Request ${decided.reference} ${decided.status.toLowerCase()}`}>
              <p className="m-0">
                We have emailed {decided.requesterName} to tell them. <Link to="/admin/requests">Review the next request</Link>
              </p>
            </SuccessBanner>
          ) : null}
        </>
      }
    >
      <p className="mb-6 flex flex-wrap items-center gap-3">
        <span className="font-bold">Status:</span> <StatusTag status={request.status} />
        <span className="nhs-body-s text-nhs-dark-grey">Submitted {formatDateTime(request.submittedAt)}</span>
      </p>

      {overlaps.length > 0 ? (
        <WarningCallout title={`${overlaps.length} other request${overlaps.length === 1 ? "" : "s"} for this building at the same time`}>
          <p className="mb-2">Check there is enough space before accepting:</p>
          <ul className="m-0 list-disc pl-5">
            {overlaps.map((o) => (
              <li key={o.id}>
                <Link to={`/admin/requests/${o.id}`}>{o.reference}</Link> – {o.requesterName},{" "}
                {formatShortPeriod(o.startsAt, o.endsAt)} ({o.status.toLowerCase()})
              </li>
            ))}
          </ul>
        </WarningCallout>
      ) : null}

      {request.status !== "Submitted" && !decided ? (
        <InsetText>
          <p className="m-0">
            {request.status} by {request.decidedByName} on {formatDateTime(request.decidedAt!)}.
          </p>
          {request.decisionNotes ? <p className="mt-2 mb-0 whitespace-pre-line">“{request.decisionNotes}”</p> : null}
        </InsetText>
      ) : null}

      <RequestDetails request={request} />

      {request.status === "Submitted" ? <DecisionPanel request={request} onDecided={setDecided} /> : null}

      <RequestTimeline request={request} />
    </Page>
  );
}
