import { useEffect, useRef, type ReactNode } from "react";
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
 * Standard page: sets the document title and moves focus to the h1 when the page opens, so
 * screen reader users know the page has changed (single-page apps do not do this by default).
 */
export function Page({ title, caption, hasError = false, before, headingAside, children }: PageProps) {
  useDocumentTitle(title, hasError);
  const headingRef = useRef<HTMLHeadingElement>(null);

  useEffect(() => {
    headingRef.current?.focus({ preventScroll: true });
  }, [title]);

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
