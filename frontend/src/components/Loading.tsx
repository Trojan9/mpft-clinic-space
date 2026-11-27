/** Loading indicator announced to screen readers, shown in place of content while data loads. */
export function Loading({ label = "Loading" }: { label?: string }) {
  return (
    <div role="status" aria-live="polite" className="flex items-center gap-3 py-8 text-nhs-dark-grey">
      <span
        aria-hidden="true"
        className="inline-block h-6 w-6 animate-spin rounded-full border-4 border-nhs-pale-grey border-t-nhs-blue"
      />
      <span>{label}…</span>
    </div>
  );
}
