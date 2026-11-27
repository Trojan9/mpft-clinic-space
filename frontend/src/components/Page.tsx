import { useEffect, useRef, type ReactNode } from "react";
import { useLocation } from "react-router";
import { useDocumentTitle } from "../lib/useDocumentTitle";

interface PageProps {
  title: string;
  caption?: string;
  hasError?: boolean;
  /** Content above the heading, such as a back link or error summary. */
  before?: ReactNode;
  headingAside?: ReactNode;
  children: ReactNode;
}

/**
 * Standard page: sets the document title and, after in-app navigation, moves focus to the h1 so
 * screen reader users know the page has changed (single-page apps do not do this by default).
 * On the first page load focus is left alone, so the first Tab still reaches the skip link.
 */
export function Page({ title, caption, hasError = false, before, headingAside, children }: PageProps) {
  useDocumentTitle(title, hasError);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const location = useLocation();
  // React Router gives the very first entry the key "default"; every navigation after that has its own key.
  const isInitialLoad = location.key === "default";

  useEffect(() => {
    if (!isInitialLoad) headingRef.current?.focus({ preventScroll: true });
  }, [title, isInitialLoad]);

  return (
    <>
      {before}
      <div className="flex flex-wrap items-start justify-between gap-x-6">
        <h1 ref={headingRef} tabIndex={-1} className="nhs-heading-xl">
          {caption ? <span className="nhs-caption-l">{caption}</span> : null}
          {title}
        </h1>
        {headingAside}
      </div>
      {children}
    </>
  );
}
