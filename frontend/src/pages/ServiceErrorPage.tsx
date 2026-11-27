import { Link } from "react-router";
import { Page } from "../components/Page";

/** Shown by the router's error boundary if a page crashes. No technical details are shown. */
export function ServiceErrorPage() {
  return (
    <div className="mx-auto max-w-[960px] px-4 py-10 sm:px-8">
      <Page title="Sorry, there is a problem with the service">
        <p>Try again later.</p>
        <p>
          <Link to="/">Go to the start page</Link>
        </p>
      </Page>
    </div>
  );
}
