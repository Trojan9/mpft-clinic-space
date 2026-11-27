import { Link } from "react-router";
import type { RequestSummary } from "../../api/types";
import { StatusTag } from "../../components/StatusTag";
import { formatShortDate, formatShortPeriod } from "../../lib/dates";

interface RequestTableProps {
  caption: string;
  requests: RequestSummary[];
  linkBase: string;
  showRequester?: boolean;
}

/**
 * Responsive table: a normal table on larger screens; on small screens each row becomes a card
 * with labels (the NHS responsive table pattern), so nothing scrolls sideways.
 */
export function RequestTable({ caption, requests, linkBase, showRequester = false }: RequestTableProps) {
  const cell = "block sm:table-cell sm:border-b sm:border-nhs-pale-grey sm:py-3 sm:pr-4 align-top";
  const label = "font-bold sm:hidden mr-1";
  return (
    <table className="mb-6 w-full border-collapse text-left sm:table">
      <caption className="sr-only">{caption}</caption>
      <thead className="sr-only sm:not-sr-only sm:table-header-group">
        <tr>
          <th scope="col" className="border-b-2 border-nhs-black py-2 pr-4">Reference</th>
          {showRequester ? <th scope="col" className="border-b-2 border-nhs-black py-2 pr-4">Requester</th> : null}
          <th scope="col" className="border-b-2 border-nhs-black py-2 pr-4">Location</th>
          <th scope="col" className="border-b-2 border-nhs-black py-2 pr-4">When</th>
          <th scope="col" className="border-b-2 border-nhs-black py-2 pr-4">Submitted</th>
          <th scope="col" className="border-b-2 border-nhs-black py-2">Status</th>
        </tr>
      </thead>
      <tbody>
        {requests.map((request) => (
          <tr key={request.id} className="mb-4 block border border-nhs-pale-grey bg-white p-4 sm:mb-0 sm:table-row sm:border-0 sm:bg-transparent sm:p-0">
            <td className={cell}>
              <span className={label}>Reference</span>
              <Link to={`${linkBase}/${request.id}`} className="font-bold">
                {request.reference}
              </Link>
            </td>
            {showRequester ? (
              <td className={cell}>
                <span className={label}>Requester</span>
                {request.requesterName}
              </td>
            ) : null}
            <td className={cell}>
              <span className={label}>Location</span>
              {request.building}
              <span className="nhs-body-s block text-nhs-dark-grey">
                {request.site}, {request.region}
              </span>
            </td>
            <td className={cell}>
              <span className={label}>When</span>
              {formatShortPeriod(request.startsAt, request.endsAt)}
            </td>
            <td className={cell}>
              <span className={label}>Submitted</span>
              {formatShortDate(request.submittedAt)}
            </td>
            <td className={`${cell} pt-2 sm:pt-3`}>
              <span className={label}>Status</span>
              <StatusTag status={request.status} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
