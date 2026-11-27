import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Navigate, useNavigate, useSearchParams } from "react-router";
import { ApiError } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { homeFor } from "../../auth/RequireRole";
import { Button } from "../../components/Button";
import { Details, InsetText } from "../../components/Callouts";
import { ErrorSummary, type ErrorSummaryItem } from "../../components/ErrorSummary";
import { TextInput } from "../../components/FormFields";
import { Loading } from "../../components/Loading";
import { Page } from "../../components/Page";

const schema = z.object({
  username: z.string().trim().min(1, "Enter your username").max(64, "Username must be 64 characters or fewer"),
  password: z.string().min(1, "Enter your password").max(128, "Password must be 128 characters or fewer"),
});
type SignInForm = z.infer<typeof schema>;

const showDemoAccounts = import.meta.env.VITE_SHOW_DEMO_ACCOUNTS === "true";

/** Only allow redirects to paths inside this app (prevents open redirects). */
function safeReturnTo(value: string | null): string | null {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) return null;
  return value;
}

export function SignInPage() {
  const { state, signIn } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [serverError, setServerError] = useState<string | null>(null);
  const reason = params.get("reason");
  const returnTo = safeReturnTo(params.get("returnTo"));

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting, submitCount },
  } = useForm<SignInForm>({ resolver: zodResolver(schema), defaultValues: { username: "", password: "" } });

  if (state.status === "loading") return <Loading label="Checking your session" />;
  if (state.status === "signed-in") return <Navigate to={returnTo ?? homeFor(state.user.role)} replace />;

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      const user = await signIn(values.username, values.password);
      navigate(returnTo ?? homeFor(user.role), { replace: true });
    } catch (error) {
      setServerError(
        error instanceof ApiError ? error.message : "Sorry, there is a problem with the service. Try again later.",
      );
    }
  });

  const summary: ErrorSummaryItem[] = [
    ...(errors.username ? [{ fieldId: "username", message: errors.username.message! }] : []),
    ...(errors.password ? [{ fieldId: "password", message: errors.password.message! }] : []),
    ...(serverError ? [{ fieldId: "username", message: serverError }] : []),
  ];

  return (
    <div className="max-w-xl">
      <Page
        title="Sign in"
        caption="MPFT Clinic Space"
        hasError={summary.length > 0}
        before={
          <>
            {submitCount > 0 || serverError ? <ErrorSummary errors={summary} /> : null}
            {reason === "expired" ? (
              <InsetText>
                <p className="m-0">You have been signed out because your session ended. Sign in again to continue.</p>
              </InsetText>
            ) : null}
            {reason === "signed-out" ? (
              <InsetText>
                <p className="m-0">You have signed out.</p>
              </InsetText>
            ) : null}
          </>
        }
      >
        <p className="mb-8">
          Use this service to request a clinical space at a Trust site, and to check the status of your requests.
        </p>

        <form noValidate onSubmit={onSubmit} className="nhs-card" aria-label="Sign in">
          <TextInput
            id="username"
            label="Username"
            hint="Your work email address"
            autoComplete="username"
            autoCapitalize="none"
            spellCheck={false}
            error={errors.username?.message}
            {...register("username")}
          />
          <TextInput
            id="password"
            label="Password"
            type="password"
            autoComplete="current-password"
            error={errors.password?.message}
            {...register("password")}
          />
          <Button type="submit" disabled={isSubmitting} className="mb-0">
            {isSubmitting ? "Signing in…" : "Sign in"}
          </Button>
        </form>

        <Details summary="Can't sign in?">
          <p className="mb-2">
            After 5 incorrect attempts your account is locked for 15 minutes. Contact the IT service desk if you
            still cannot sign in.
          </p>
          <p className="m-0">In live use this service would use your NHS Microsoft account (single sign-on).</p>
        </Details>

        {showDemoAccounts ? (
          <section aria-labelledby="demo-accounts" className="border-2 border-dashed border-nhs-mid-grey bg-white p-4">
            <h2 id="demo-accounts" className="nhs-heading-s mb-2">
              Test accounts from the exercise data (local development only)
            </h2>
            <ul className="nhs-body-s m-0 list-disc space-y-1 pl-5">
              <li>
                Requester: <code>alex.evans@example.nhs.uk</code> with password <code>Password123!</code>
              </li>
              <li>
                Administrator: <code>pat.jones@example.nhs.uk</code> with password <code>AdminPass!23</code>
              </li>
            </ul>
            <p className="nhs-body-s mt-2 mb-0">All 14 accounts in the supplied Users sheet can sign in.</p>
          </section>
        ) : null}
      </Page>
    </div>
  );
}
