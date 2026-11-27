import { Link } from "react-router";

export function BackLink({ to, children = "Back" }: { to: string; children?: string }) {
  return (
    <Link to={to} className="mb-6 inline-flex items-center gap-1 text-base visited:text-nhs-blue">
      <svg aria-hidden="true" focusable="false" viewBox="0 0 24 24" className="h-5 w-5 fill-current">
        <path d="M8.5 12c0-.3.1-.5.3-.7l5-5c.4-.4 1-.4 1.4 0s.4 1 0 1.4L10.9 12l4.3 4.3c.4.4.4 1 0 1.4s-1 .4-1.4 0l-5-5c-.2-.2-.3-.4-.3-.7z" />
      </svg>
      {children}
    </Link>
  );
}
