import type { RequestDetail } from "../../api/types";
import { SummaryList } from "../../components/SummaryList";
import { formatDateTime, formatPeriod } from "../../lib/dates";

/** The read-only body of a request, shared by the requester and administrator detail pages. */
export function RequestDetails({ request }: { request: RequestDetail }) {
  return (
    <>
      <h2 className="nhs-heading-m">Requester</h2>
      <SummaryList
        rows={[
          { key: "Name", value: request.requesterName },
          { key: "Email address", value: request.requesterEmail },
        ]}
      />

      <h2 className="nhs-heading-m">Budget</h2>
      <SummaryList
        rows={[
          { key: "Cost code", value: request.costCode },
          { key: "Budget holder", value: request.budgetHolderName },
          { key: "Budget holder email", value: request.budgetHolderEmail },
        ]}
      />

      <h2 className="nhs-heading-m">Location and time</h2>
      <SummaryList
        rows={[
          { key: "Region", value: request.location.region },
          { key: "Site", value: request.location.site },
          { key: "Building", value: request.location.building },
          { key: "When", value: formatPeriod(request.startsAt, request.endsAt) },
        ]}
      />

      <h2 className="nhs-heading-m">Requirements</h2>
      <SummaryList
        rows={[
          { key: "Sink required", value: request.sinkRequired ? "Yes" : "No" },
          { key: "Clinical couch required", value: request.clinicalCouchRequired ? "Yes" : "No" },
          { key: "Other requirements", value: request.additionalRequirements ?? "None" },
          { key: "Terms and conditions", value: `Accepted ${formatDateTime(request.termsAcceptedAt)} (version ${request.termsVersion})` },
        ]}
      />
    </>
  );
}

/** Audit trail: who did what, and when. */
export function RequestTimeline({ request }: { request: RequestDetail }) {
  return (
    <section aria-labelledby="history">
      <h2 id="history" className="nhs-heading-m">
        History
      </h2>
      <ol className="mb-8 border-l-4 border-nhs-pale-grey pl-6">
        {[...request.timeline].reverse().map((event) => (
          <li key={`${event.type}-${event.occurredAt}`} className="relative mb-6 last:mb-0">
            <span aria-hidden="true" className="absolute top-2 -left-[34px] h-4 w-4 rounded-full border-4 border-white bg-nhs-blue" />
            <p className="m-0 font-bold">
              {event.type === "Submitted" ? "Request submitted" : `Request ${event.type.toLowerCase()}`} by {event.actorName}
            </p>
            <p className="nhs-body-s m-0 text-nhs-dark-grey">{formatDateTime(event.occurredAt)}</p>
            {event.notes ? <p className="mt-1 mb-0 whitespace-pre-line">“{event.notes}”</p> : null}
          </li>
        ))}
      </ol>
    </section>
  );
}
