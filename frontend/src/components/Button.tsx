import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Link, type LinkProps } from "react-router";

type Variant = "primary" | "secondary" | "warning" | "reverse";

// NHS button: solid colour with a darker bottom "shadow" that moves on press.
const variants: Record<Variant, string> = {
  primary: "bg-nhs-green text-white shadow-[0_4px_0_#00401e] hover:bg-[#00662f]",
  secondary: "bg-white text-nhs-blue border-2 border-nhs-blue shadow-[0_4px_0_#005eb8] hover:bg-[#f0f4f5]",
  warning: "bg-nhs-red text-white shadow-[0_4px_0_#6b140e] hover:bg-[#aa2016]",
  reverse: "bg-white text-nhs-black shadow-[0_4px_0_#212b32] hover:bg-nhs-light-grey",
};

const base =
  "relative inline-flex items-center justify-center gap-2 rounded px-4 py-2.5 font-bold no-underline " +
  "transition-[top] active:top-[2px] active:shadow-none disabled:cursor-not-allowed disabled:opacity-50 " +
  "mb-6 w-full sm:w-auto text-base sm:text-[1.1875rem] leading-snug";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  children: ReactNode;
}

export function Button({ variant = "primary", className = "", type = "button", ...props }: ButtonProps) {
  return <button type={type} className={`${base} ${variants[variant]} ${className}`} {...props} />;
}

interface ButtonLinkProps extends LinkProps {
  variant?: Variant;
}

/** A link that looks like a button, for actions that go to another page. */
export function ButtonLink({ variant = "primary", className = "", ...props }: ButtonLinkProps) {
  return (
    <Link
      className={`${base} ${variants[variant]} visited:text-current hover:text-current ${className}`}
      {...props}
    />
  );
}
