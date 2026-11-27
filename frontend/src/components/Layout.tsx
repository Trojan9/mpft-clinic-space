import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router";
import { useAuth } from "../auth/AuthContext";

function SkipLink() {
  return (
    <a
      href="#maincontent"
      className="sr-only-focusable absolute top-2 left-2 z-50 bg-nhs-yellow p-3 font-bold text-nhs-black"
      onClick={(event) => {
        event.preventDefault();
        const main = document.getElementById("maincontent");
        main?.focus();
      }}
    >
      Skip to main content
    </a>
  );
}

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `block px-1 py-3 text-white no-underline visited:text-white hover:text-white hover:underline sm:py-4 ${
    isActive ? "font-bold underline decoration-4 underline-offset-[10px]" : ""
  }`;

function Header() {
  const { state, signOut } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const user = state.status === "signed-in" ? state.user : null;

  const links =
    user?.role === "Administrator"
      ? [{ to: "/admin/requests", label: "All requests" }]
      : user
        ? [
            { to: "/requests", label: "My requests", end: true },
            { to: "/requests/new", label: "New request" },
          ]
        : [];

  return (
    <header className="bg-nhs-blue text-white" role="banner">
      <div className="mx-auto flex max-w-[960px] flex-wrap items-center justify-between gap-2 px-4 py-3 sm:px-8">
        <NavLink
          to="/"
          className="flex items-center gap-3 text-white no-underline visited:text-white hover:text-white"
        >
          <span aria-hidden="true" className="flex h-10 w-10 items-center justify-center rounded bg-white text-lg font-black text-nhs-blue">
            CS
          </span>
          <span className="text-xl leading-tight font-bold sm:text-2xl">
            MPFT Clinic Space
          </span>
        </NavLink>

        {user ? (
          <div className="flex items-center gap-4">
            <p className="m-0 hidden text-right text-sm leading-tight sm:block" data-testid="signed-in-user">
              <span className="block font-bold">{user.displayName}</span>
              <span className="block text-[#d6e5f4]">{user.role}</span>
            </p>
            <button
              type="button"
              className="rounded border-2 border-white px-3 py-1.5 font-bold text-white hover:bg-nhs-dark-blue"
              onClick={async () => {
                await signOut();
                navigate("/sign-in?reason=signed-out", { replace: true });
              }}
            >
              Sign out
            </button>
            <button
              type="button"
              className="rounded border-2 border-white px-3 py-1.5 font-bold text-white sm:hidden"
              aria-expanded={menuOpen}
              aria-controls="header-navigation"
              onClick={() => setMenuOpen((open) => !open)}
            >
              Menu
            </button>
          </div>
        ) : null}
      </div>

      {links.length ? (
        <nav
          id="header-navigation"
          aria-label="Primary navigation"
          className={`border-t border-[#2f73c9] bg-nhs-blue ${menuOpen ? "block" : "hidden"} sm:block`}
        >
          <ul className="mx-auto flex max-w-[960px] flex-col gap-0 px-4 sm:flex-row sm:gap-8 sm:px-8">
            {links.map((link) => (
              <li key={link.to}>
                <NavLink to={link.to} end={link.end} className={navLinkClass} onClick={() => setMenuOpen(false)}>
                  {link.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      ) : null}
    </header>
  );
}

function Footer() {
  return (
    <footer className="mt-auto border-t-4 border-nhs-blue bg-nhs-light-grey" role="contentinfo">
      <div className="mx-auto max-w-[960px] px-4 py-8 text-nhs-dark-grey sm:px-8">
        <p className="nhs-body-s m-0">
          MPFT Clinic Space is an internal service for Midlands Partnership University NHS Foundation Trust staff.
        </p>
        <p className="nhs-body-s mt-2 mb-0">Technical exercise build. Not a live NHS service.</p>
      </div>
    </footer>
  );
}

export function Layout() {
  return (
    <div className="flex min-h-screen flex-col">
      <SkipLink />
      <Header />
      <div className="mx-auto w-full max-w-[960px] flex-1 px-4 pt-6 pb-12 sm:px-8 sm:pt-10">
        <main id="maincontent" tabIndex={-1} className="outline-none">
          <Outlet />
        </main>
      </div>
      <Footer />
    </div>
  );
}
