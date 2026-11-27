interface PaginationProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

/** NHS-style "Previous / Next" pagination with the current position spelt out. */
export function Pagination({ page, totalPages, onChange }: PaginationProps) {
  if (totalPages <= 1) return null;
  return (
    <nav aria-label="Pagination" className="mt-6 mb-8 flex flex-wrap items-center justify-between gap-4">
      <div>
        {page > 1 ? (
          <button type="button" className="text-nhs-blue underline underline-offset-2 hover:text-nhs-link-hover" onClick={() => onChange(page - 1)}>
            ← Previous<span className="sr-only"> page</span>
          </button>
        ) : null}
      </div>
      <p className="m-0 text-nhs-dark-grey" aria-current="page">
        Page {page} of {totalPages}
      </p>
      <div>
        {page < totalPages ? (
          <button type="button" className="text-nhs-blue underline underline-offset-2 hover:text-nhs-link-hover" onClick={() => onChange(page + 1)}>
            Next<span className="sr-only"> page</span> →
          </button>
        ) : null}
      </div>
    </nav>
  );
}
