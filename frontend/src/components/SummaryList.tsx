import type { ReactNode } from "react";
import { Link } from "react-router";

export interface SummaryRow {
  key: string;
  value: ReactNode;
  /** Optional "Change" link, used on the check answers page. */
  change?: { to: string; label: string };
}

/** NHS summary list: key/value pairs in a description list. */
export function SummaryList({ rows, className = "" }: { rows: SummaryRow[]; className?: string }) {
  return (
    <dl className={`mb-8 sm:table sm:w-full sm:table-fixed ${className}`}>
      {rows.map((row) => (
        <div key={row.key} className="mb-4 border-b border-nhs-pale-grey pb-3 sm:mb-0 sm:table-row sm:pb-0">
          <dt className="mb-1 font-bold sm:table-cell sm:w-[35%] sm:border-b sm:border-nhs-pale-grey sm:py-3 sm:pr-5">
            {row.key}
          </dt>
          <dd className="break-words whitespace-pre-line sm:table-cell sm:border-b sm:border-nhs-pale-grey sm:py-3 sm:pr-5">
            {row.value}
          </dd>
          {row.change ? (
            <dd className="mt-2 sm:mt-0 sm:table-cell sm:w-[20%] sm:border-b sm:border-nhs-pale-grey sm:py-3 sm:text-right">
              <Link to={row.change.to}>
                Change<span className="sr-only"> {row.change.label}</span>
              </Link>
            </dd>
          ) : null}
        </div>
      ))}
    </dl>
  );
}
