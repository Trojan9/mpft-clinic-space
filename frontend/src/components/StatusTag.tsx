import type { RequestStatus } from "../api/types";

const styles: Record<RequestStatus, string> = {
  Submitted: "bg-tag-blue-bg text-tag-blue-text border-tag-blue-text",
  Accepted: "bg-tag-green-bg text-tag-green-text border-tag-green-text",
  Rejected: "bg-tag-red-bg text-tag-red-text border-tag-red-text",
};

/** NHS tag. Colour is never the only signal: the status word is always shown. */
export function StatusTag({ status }: { status: RequestStatus }) {
  return (
    <strong
      className={`inline-block border px-2 py-0.5 text-sm leading-tight font-bold whitespace-nowrap sm:text-base ${styles[status]}`}
      data-testid="status-tag"
    >
      {status}
    </strong>
  );
}
