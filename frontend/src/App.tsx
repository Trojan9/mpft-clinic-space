import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createBrowserRouter, RouterProvider, type RouteObject } from "react-router";
import { ApiError } from "./api/client";
import { AuthProvider } from "./auth/AuthContext";
import { HomeRedirect, RequireRole } from "./auth/RequireRole";
import { Layout } from "./components/Layout";
import { AdminRequestDetailPage } from "./features/admin/AdminRequestDetailPage";
import { AdminRequestsPage } from "./features/admin/AdminRequestsPage";
import { SignInPage } from "./features/auth/SignInPage";
import { CheckAnswersPage } from "./features/requests/CheckAnswersPage";
import { ConfirmationPage } from "./features/requests/ConfirmationPage";
import { MyRequestsPage } from "./features/requests/MyRequestsPage";
import { RequestDetailPage } from "./features/requests/RequestDetailPage";
import { RequestDraftProvider } from "./features/requests/RequestDraft";
import { RequestFormPage } from "./features/requests/RequestFormPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ServiceErrorPage } from "./pages/ServiceErrorPage";

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    errorElement: <ServiceErrorPage />,
    children: [
      { index: true, element: <HomeRedirect /> },
      { path: "sign-in", element: <SignInPage /> },
      {
        element: <RequireRole allow="Requester" />,
        children: [
          { path: "requests", element: <MyRequestsPage /> },
          {
            path: "requests/new",
            element: <RequestDraftProvider />,
            children: [
              { index: true, element: <RequestFormPage /> },
              { path: "check", element: <CheckAnswersPage /> },
              { path: "confirmation", element: <ConfirmationPage /> },
            ],
          },
          { path: "requests/:id", element: <RequestDetailPage /> },
        ],
      },
      {
        element: <RequireRole allow="Administrator" />,
        children: [
          { path: "admin/requests", element: <AdminRequestsPage /> },
          { path: "admin/requests/:id", element: <AdminRequestDetailPage /> },
        ],
      },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Do not retry "not found", "forbidden" or "signed out"; do retry brief network blips.
        retry: (count, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && count < 2,
        refetchOnWindowFocus: true,
        staleTime: 15_000,
      },
    },
  });
}

const router = createBrowserRouter(routes);
const queryClient = createQueryClient();

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>
  );
}
