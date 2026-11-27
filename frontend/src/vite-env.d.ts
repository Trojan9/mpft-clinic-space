/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** "true" shows the demo account list on the sign-in page (local builds only). */
  readonly VITE_SHOW_DEMO_ACCOUNTS?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
