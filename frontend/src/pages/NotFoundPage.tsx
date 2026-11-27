import { Link } from "react-router";
import { Page } from "../components/Page";

export function NotFoundPage() {
  return (
    <Page title="Page not found">
      <p>If you typed the web address, check it is correct.</p>
      <p>If you pasted the web address, check you copied the entire address.</p>
      <p>
        If you followed a link to a request, it may not exist or you may not have permission to see it.{" "}
        <Link to="/">Go to the start page</Link>.
      </p>
    </Page>
  );
}
