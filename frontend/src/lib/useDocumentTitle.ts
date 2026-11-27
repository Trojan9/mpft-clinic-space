import { useEffect } from "react";

const SERVICE = "MPFT Clinic Space";

/**
 * Sets the browser tab title, which is the first thing a screen reader announces on a new page.
 * Pages with form errors get an "Error: " prefix, as the NHS service manual recommends.
 */
export function useDocumentTitle(title: string, hasError = false) {
  useEffect(() => {
    document.title = `${hasError ? "Error: " : ""}${title} - ${SERVICE}`;
  }, [title, hasError]);
}
