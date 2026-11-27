/** Placeholder terms, as the exercise brief asks. Real wording would come from the Trust's estates team. */
export const TERMS_VERSION = "2026-11-v1";

export function TermsAndConditions() {
  return (
    <div className="nhs-body-s space-y-2">
      <p className="m-0 font-bold">Placeholder terms and conditions (version {TERMS_VERSION})</p>
      <ol className="list-decimal space-y-1 pl-5">
        <li>The space must only be used for the clinical activity described in this request.</li>
        <li>You must leave the space clean and tidy, and report any damage or faults to the site team.</li>
        <li>Charges for the space will be made to the cost code you give, with the budget holder's approval.</li>
        <li>Infection prevention and control procedures for the site must be followed at all times.</li>
        <li>The Trust may cancel or move a booking for operational reasons and will tell you as soon as possible.</li>
      </ol>
    </div>
  );
}
