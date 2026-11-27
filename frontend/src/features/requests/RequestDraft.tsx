import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { Outlet } from "react-router";
import type { RequestFormValues } from "./requestSchema";

export interface LocationLabels {
  region: string;
  site: string;
  building: string;
}

export interface RequestDraft {
  values: RequestFormValues;
  labels: LocationLabels;
}

interface DraftContextValue {
  draft: RequestDraft | null;
  setDraft: (draft: RequestDraft | null) => void;
  serverErrors: Record<string, string>;
  setServerErrors: (errors: Record<string, string>) => void;
}

const DraftContext = createContext<DraftContextValue | null>(null);

/**
 * Holds the request between the form and the check answers page. Kept in memory only (not in
 * browser storage) so no personal data is left behind on a shared computer.
 */
export function RequestDraftProvider({ children }: { children?: ReactNode }) {
  const [draft, setDraft] = useState<RequestDraft | null>(null);
  const [serverErrors, setServerErrors] = useState<Record<string, string>>({});
  const value = useMemo(() => ({ draft, setDraft, serverErrors, setServerErrors }), [draft, serverErrors]);
  return <DraftContext.Provider value={value}>{children ?? <Outlet />}</DraftContext.Provider>;
}

export function useRequestDraft(): DraftContextValue {
  const context = useContext(DraftContext);
  if (!context) throw new Error("useRequestDraft must be used inside RequestDraftProvider");
  return context;
}
