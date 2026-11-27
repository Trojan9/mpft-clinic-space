import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Navigate, useNavigate } from "react-router";
import { ApiError } from "../../api/client";
import { requestsApi } from "../../api/endpoints";
import { useCurrentUser } from "../../auth/AuthContext";
import { BackLink } from "../../components/BackLink";
import { Button } from "../../components/Button";
import { ErrorSummary } from "../../components/ErrorSummary";
import { Page } from "../../components/Page";
import { SummaryList } from "../../components/SummaryList";
import { formatPeriod } from "../../lib/dates";
import { useRequestDraft } from "./RequestDraft";
import { API_FIELD_TO_FORM_FIELD, toPayload } from "./requestSchema";

/** NHS "check your answers" pattern: one last look, with a Change link for every answer. */
export function CheckAnswersPage() {
  const user = useCurrentUser();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { draft, setServerErrors } = useRequestDraft();
  const [error, setError] = useState<string | null>(null);

  const submit = useMutation({
    mutationFn: requestsApi.create,
    onSuccess: (created) => {
      // The confirmation page clears the draft once it has mounted. Clearing it here would
      // re-render this page without a draft before the navigation lands, and the guard below
      // would send the person back to an empty form.
      void queryClient.invalidateQueries({ queryKey: ["requests"] });
      navigate("/requests/new/confirmation", { replace: true, state: { id: created.id, reference: created.reference } });
    },
    onError: (failure) => {
      if (failure instanceof ApiError && failure.status === 400 && Object.keys(failure.fieldErrors).length > 0) {
        // Send the person back to the form with the server's messages against each field.
        const mapped: Record<string, string> = {};
        for (const [field, message] of Object.entries(failure.fieldErrors)) {
          mapped[API_FIELD_TO_FORM_FIELD[field] ?? "costCode"] = message;
        }
        setServerErrors(mapped);
        navigate("/requests/new");
        return;
      }
      setError(failure instanceof ApiError ? failure.message : "Sorry, there is a problem with the service. Try again later.");
    },
  });

  if (!draft) return submit.isSuccess ? null : <Navigate to="/requests/new" replace />;

  const { values, labels } = draft;
  const payload = toPayload(values);
  const change = (field: string, label: string) => ({ to: `/requests/new#${field}`, label });

  return (
    <Page
      title="Check your answers before submitting your request"
      hasError={Boolean(error)}
      before={
        <>
          <BackLink to="/requests/new">Back</BackLink>
          {error ? <ErrorSummary errors={[{ message: error }]} /> : null}
        </>
      }
    >
      <h2 className="nhs-heading-m">Your details</h2>
      <SummaryList
        rows={[
          { key: "Name", value: user.displayName },
          { key: "Email address", value: user.email },
        ]}
      />

      <h2 className="nhs-heading-m">Budget</h2>
      <SummaryList
        rows={[
          { key: "Cost code", value: payload.costCode, change: change("costCode", "cost code") },
          { key: "Budget holder", value: payload.budgetHolderName, change: change("budgetHolderName", "budget holder name") },
          { key: "Budget holder email", value: payload.budgetHolderEmail, change: change("budgetHolderEmail", "budget holder email address") },
        ]}
      />

      <h2 className="nhs-heading-m">Location</h2>
      <SummaryList
        rows={[
          { key: "Region", value: labels.region, change: change("regionId", "region") },
          { key: "Site", value: labels.site, change: change("siteId", "site") },
          { key: "Building", value: labels.building, change: change("buildingId", "building") },
        ]}
      />

      <h2 className="nhs-heading-m">Date and time</h2>
      <SummaryList
        rows={[{ key: "When", value: formatPeriod(payload.startsAt, payload.endsAt), change: change("startDay", "date and time") }]}
      />

      <h2 className="nhs-heading-m">Requirements</h2>
      <SummaryList
        rows={[
          { key: "Sink required", value: payload.sinkRequired ? "Yes" : "No", change: change("sinkRequired", "whether a sink is required") },
          { key: "Clinical couch required", value: payload.clinicalCouchRequired ? "Yes" : "No", change: change("clinicalCouchRequired", "whether a clinical couch is required") },
          { key: "Other requirements", value: payload.additionalRequirements ?? "None", change: change("additionalRequirements", "other requirements") },
        ]}
      />

      <h2 className="nhs-heading-m">Now submit your request</h2>
      <p className="mb-6 max-w-2xl">
        You have accepted the terms and conditions. When you submit, an administrator will review your request and
        we will email you at <strong>{user.email}</strong> when it has been accepted or rejected.
      </p>
      <Button type="button" disabled={submit.isPending} onClick={() => submit.mutate(payload)}>
        {submit.isPending ? "Submitting…" : "Submit request"}
      </Button>
    </Page>
  );
}
