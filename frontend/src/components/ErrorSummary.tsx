import { useEffect, useRef } from "react";

export interface ErrorSummaryItem {
  /** id of the field to move focus to when the link is clicked. */
  fieldId?: string;
  message: string;
}

/**
 * NHS error summary. Shown at the top of the page when a form has errors. It takes focus so screen
 * reader users hear about the problem at once, and each item links to the field that needs fixing.
 */
export function ErrorSummary({ errors, title = "There is a problem" }: { errors: ErrorSummaryItem[]; title?: string }) {
  const ref = useRef<HTMLDivElement>(null);
  const signature = errors.map((e) => `${e.fieldId}:${e.message}`).join("|");

  useEffect(() => {
    if (errors.length > 0) ref.current?.focus();
    // Refocus only when the set of errors changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [signature]);

  if (errors.length === 0) return null;

  return (
    <div
      ref={ref}
      tabIndex={-1}
      role="alert"
      aria-labelledby="error-summary-title"
      className="mb-8 border-4 border-nhs-red bg-white p-4 sm:p-6 focus:outline-4 focus:outline-nhs-yellow"
    >
      <h2 id="error-summary-title" className="nhs-heading-m">
        {title}
      </h2>
      <ul className="space-y-2">
        {errors.map((error) => (
          <li key={`${error.fieldId}-${error.message}`}>
            {error.fieldId ? (
              <a
                href={`#${error.fieldId}`}
                className="font-bold text-nhs-red visited:text-nhs-red hover:text-nhs-dark-red"
                onClick={(event) => {
                  const field = document.getElementById(error.fieldId!);
                  if (field) {
                    event.preventDefault();
                    field.focus();
                    field.scrollIntoView({ block: "center" });
                  }
                }}
              >
                {error.message}
              </a>
            ) : (
              <span className="font-bold text-nhs-red">{error.message}</span>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
