import type { ReactNode } from "react";

/** NHS inset text: draws attention to a short piece of information. */
export function InsetText({ children }: { children: ReactNode }) {
  return <div className="mb-8 border-l-[10px] border-nhs-blue bg-white py-3 pl-4 sm:pl-6">{children}</div>;
}

/** NHS warning callout: important information the user must read. */
export function WarningCallout({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="mb-8 border border-nhs-warm-yellow bg-[#fff9c4]">
      <h2 className="m-0 inline-block bg-nhs-warm-yellow px-4 py-2 text-[1.125rem] font-bold sm:px-6">
        <span className="sr-only">Important: </span>
        {title}
      </h2>
      <div className="px-4 pt-2 pb-4 sm:px-6">{children}</div>
    </div>
  );
}

/** Success message after an action, announced politely to screen readers. */
export function SuccessBanner({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div role="status" className="mb-8 border-l-[10px] border-nhs-green bg-white p-4 sm:p-6">
      <h2 className="nhs-heading-m mb-2 text-nhs-dark-green">{title}</h2>
      {children}
    </div>
  );
}

/** NHS details: progressive disclosure of extra information. */
export function Details({ summary, children }: { summary: string; children: ReactNode }) {
  return (
    <details className="group mb-8">
      <summary className="cursor-pointer text-nhs-blue underline underline-offset-2 hover:text-nhs-link-hover">
        {summary}
      </summary>
      <div className="mt-3 border-l-4 border-nhs-pale-grey py-1 pl-4">{children}</div>
    </details>
  );
}

/** Shown when data cannot be loaded. */
export function ProblemPanel({ message, children }: { message: string; children?: ReactNode }) {
  return (
    <div role="alert" className="mb-8 border-4 border-nhs-red bg-white p-4 sm:p-6">
      <h2 className="nhs-heading-m">There is a problem</h2>
      <p className="mb-2">{message}</p>
      {children}
    </div>
  );
}
