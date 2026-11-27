import { useEffect, useRef } from "react";
import { Link, Navigate, useLocation } from "react-router";
import { useCurrentUser } from "../../auth/AuthContext";
import { ButtonLink } from "../../components/Button";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { useRequestDraft } from "./RequestDraft";

interface ConfirmationState {
  id: string;
  reference: string;
}

/** NHS confirmation page: a green panel with the reference number, then what happens next. */
export function ConfirmationPage() {
  const user = useCurrentUser();
  const state = useLocation().state as ConfirmationState | null;
  const headingRef = useRef<HTMLHeadingElement>(null);
  const { setDraft } = useRequestDraft();
  useDocumentTitle("Request submitted");
  useEffect(() => headingRef.current?.focus(), []);
  // The request is saved, so forget the answers (kept in memory only, never in browser storage).
  useEffect(() => setDraft(null), [setDraft]);

  if (!state?.id) return <Navigate to="/requests" replace />;

  return (
    <>
      <div className="mb-8 bg-nhs-green p-6 text-center text-white sm:p-10">
        <h1 ref={headingRef} tabIndex={-1} className="nhs-heading-xl mb-4 sm:mb-6">
          Request submitted
        </h1>
        <p className="m-0 text-xl sm:text-2xl">
          Your reference number
          <br />
          <strong className="text-2xl sm:text-[2rem]" data-testid="confirmation-reference">
            {state.reference}
          </strong>
        </p>
      </div>
      <p>We have sent a confirmation to {user.email}.</p>
      <h2 className="nhs-heading-m">What happens next</h2>
      <p>An administrator will review your request. We will email you when it has been accepted or rejected.</p>
      <p className="mb-8">
        You can check its status at any time in <Link to="/requests">My requests</Link>.
      </p>
      <div className="flex flex-wrap gap-4">
        <ButtonLink to={`/requests/${state.id}`} variant="secondary">
          View your request
        </ButtonLink>
        <ButtonLink to="/requests/new" variant="secondary">
          Make another request
        </ButtonLink>
      </div>
    </>
  );
}
