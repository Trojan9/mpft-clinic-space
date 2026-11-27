import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";

/** aria-describedby value that links a field to its hint and error message. */
function describedBy(id: string, hint?: ReactNode, error?: string): string | undefined {
  const ids = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean);
  return ids.length ? ids.join(" ") : undefined;
}

function Hint({ id, children }: { id: string; children?: ReactNode }) {
  return children ? (
    <div id={`${id}-hint`} className="nhs-hint">
      {children}
    </div>
  ) : null;
}

function ErrorMessage({ id, message }: { id: string; message?: string }) {
  return message ? (
    <span id={`${id}-error`} className="nhs-error-message">
      <span className="sr-only">Error: </span>
      {message}
    </span>
  ) : null;
}

function FormGroup({ error, children }: { error?: string; children: ReactNode }) {
  return <div className={`mb-6 sm:mb-8 ${error ? "nhs-form-group-error" : ""}`}>{children}</div>;
}

interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: ReactNode;
  error?: string;
  widthClass?: string;
}

export function TextInput({ id, label, hint, error, widthClass = "max-w-xl", ...input }: TextInputProps) {
  return (
    <FormGroup error={error}>
      <label htmlFor={id} className="mb-1 block font-bold">
        {label}
      </label>
      <Hint id={id}>{hint}</Hint>
      <ErrorMessage id={id} message={error} />
      <input
        id={id}
        className={`nhs-input ${widthClass} ${error ? "nhs-input-error" : ""}`}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(id, hint, error)}
        {...input}
      />
    </FormGroup>
  );
}

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  id: string;
  label: string;
  hint?: ReactNode;
  error?: string;
  maxChars?: number;
  currentLength?: number;
}

/** Textarea with the NHS character count pattern when maxChars is given. */
export function Textarea({ id, label, hint, error, maxChars, currentLength = 0, ...textarea }: TextareaProps) {
  const remaining = maxChars !== undefined ? maxChars - currentLength : undefined;
  return (
    <FormGroup error={error}>
      <label htmlFor={id} className="mb-1 block font-bold">
        {label}
      </label>
      <Hint id={id}>{hint}</Hint>
      <ErrorMessage id={id} message={error} />
      <textarea
        id={id}
        rows={5}
        className={`nhs-input max-w-2xl ${error ? "nhs-input-error" : ""}`}
        aria-invalid={error ? true : undefined}
        aria-describedby={[describedBy(id, hint, error), maxChars ? `${id}-count` : null].filter(Boolean).join(" ") || undefined}
        {...textarea}
      />
      {remaining !== undefined ? (
        <div
          id={`${id}-count`}
          aria-live="polite"
          className={`mt-1 ${remaining < 0 ? "font-bold text-nhs-red" : "text-nhs-dark-grey"}`}
        >
          {remaining >= 0
            ? `You have ${remaining} character${remaining === 1 ? "" : "s"} remaining`
            : `You have ${-remaining} character${remaining === -1 ? "" : "s"} too many`}
        </div>
      ) : null}
    </FormGroup>
  );
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  id: string;
  label: string;
  hint?: ReactNode;
  error?: string;
  options: { value: string | number; label: string }[];
  placeholder?: string;
}

export function Select({ id, label, hint, error, options, placeholder = "Select an option", ...select }: SelectProps) {
  return (
    <FormGroup error={error}>
      <label htmlFor={id} className="mb-1 block font-bold">
        {label}
      </label>
      <Hint id={id}>{hint}</Hint>
      <ErrorMessage id={id} message={error} />
      <select
        id={id}
        className={`nhs-input max-w-xl ${error ? "nhs-input-error" : ""} disabled:bg-nhs-light-grey disabled:text-nhs-dark-grey`}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(id, hint, error)}
        {...select}
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </FormGroup>
  );
}

interface RadiosProps {
  id: string;
  legend: string;
  hint?: ReactNode;
  error?: string;
  inline?: boolean;
  options: { value: string; label: string; hint?: string }[];
  /** Props from react-hook-form's register(), applied to every radio. */
  inputProps: InputHTMLAttributes<HTMLInputElement>;
  legendSize?: "s" | "m";
}

/** NHS radios inside a fieldset. The first radio gets the group id, so error links focus it. */
export function Radios({ id, legend, hint, error, inline = false, options, inputProps, legendSize = "s" }: RadiosProps) {
  return (
    <FormGroup error={error}>
      <fieldset aria-describedby={describedBy(id, hint, error)}>
        <legend className={`mb-2 ${legendSize === "m" ? "nhs-heading-m" : "font-bold"}`}>{legend}</legend>
        <Hint id={id}>{hint}</Hint>
        <ErrorMessage id={id} message={error} />
        <div className={inline ? "flex flex-wrap gap-x-6" : "space-y-2"}>
          {options.map((option, index) => {
            const optionId = index === 0 ? id : `${id}-${option.value}`;
            return (
              <div key={option.value} className="relative flex items-start py-1">
                <input
                  id={optionId}
                  type="radio"
                  value={option.value}
                  className="peer h-10 w-10 shrink-0 cursor-pointer appearance-none rounded-full border-2 border-nhs-black bg-white checked:bg-[radial-gradient(circle,_#212b32_0_45%,_#fff_47%)]"
                  aria-describedby={option.hint ? `${optionId}-item-hint` : undefined}
                  {...inputProps}
                />
                <div className="px-3 pt-1.5">
                  <label htmlFor={optionId} className="cursor-pointer">
                    {option.label}
                  </label>
                  {option.hint ? (
                    <div id={`${optionId}-item-hint`} className="text-nhs-dark-grey">
                      {option.hint}
                    </div>
                  ) : null}
                </div>
              </div>
            );
          })}
        </div>
      </fieldset>
    </FormGroup>
  );
}

interface CheckboxProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: ReactNode;
  error?: string;
}

export function Checkbox({ id, label, error, ...input }: CheckboxProps) {
  return (
    <FormGroup error={error}>
      <ErrorMessage id={id} message={error} />
      <div className="relative flex items-start">
        <input
          id={id}
          type="checkbox"
          className="h-10 w-10 shrink-0 cursor-pointer appearance-none border-2 border-nhs-black bg-white checked:bg-[url('data:image/svg+xml;utf8,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22><path d=%22M4 12.5l5 5L20 6.5%22 stroke=%22%23212b32%22 stroke-width=%223.5%22 fill=%22none%22/></svg>')] checked:bg-center checked:bg-no-repeat"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${id}-error` : undefined}
          {...input}
        />
        <label htmlFor={id} className="cursor-pointer px-3 pt-1.5">
          {label}
        </label>
      </div>
    </FormGroup>
  );
}

const HOURS = Array.from({ length: 24 }, (_, h) => ({ value: String(h).padStart(2, "0"), label: String(h).padStart(2, "0") }));
const MINUTES = ["00", "15", "30", "45"].map((m) => ({ value: m, label: m }));

interface DateTimeInputProps {
  id: string;
  legend: string;
  hint?: ReactNode;
  dateError?: string;
  timeError?: string;
  register: (field: "Day" | "Month" | "Year" | "Hour" | "Minute") => InputHTMLAttributes<HTMLInputElement> & SelectHTMLAttributes<HTMLSelectElement>;
}

/**
 * NHS date input (separate day, month and year boxes) plus a 24-hour time picked from two
 * drop-downs in 15-minute steps. Separate boxes avoid the accessibility problems of native date pickers.
 */
export function DateTimeInput({ id, legend, hint, dateError, timeError, register }: DateTimeInputProps) {
  const error = dateError ?? timeError;
  const box = (field: "Day" | "Month" | "Year", label: string, width: string, invalid: boolean) => (
    <div className="flex flex-col">
      <label htmlFor={`${id}${field}`} className="mb-1">
        {label}
      </label>
      <input
        id={`${id}${field}`}
        inputMode="numeric"
        autoComplete="off"
        className={`nhs-input ${width} ${invalid ? "nhs-input-error" : ""}`}
        aria-invalid={invalid || undefined}
        {...register(field)}
      />
    </div>
  );

  return (
    <FormGroup error={error}>
      <fieldset aria-describedby={describedBy(id, hint, error)}>
        <legend className="mb-2 font-bold">{legend}</legend>
        <Hint id={id}>{hint}</Hint>
        <ErrorMessage id={id} message={dateError ?? timeError} />
        <div className="mb-4 flex flex-wrap gap-4">
          {box("Day", "Day", "w-16", Boolean(dateError))}
          {box("Month", "Month", "w-16", Boolean(dateError))}
          {box("Year", "Year", "w-24", Boolean(dateError))}
        </div>
        {timeError && dateError ? <ErrorMessage id={`${id}-time`} message={timeError} /> : null}
        <div className="flex flex-wrap items-end gap-2">
          <div className="flex flex-col">
            <label htmlFor={`${id}Hour`} className="mb-1">
              Hour
            </label>
            <select id={`${id}Hour`} className={`nhs-input w-24 ${timeError ? "nhs-input-error" : ""}`} aria-invalid={Boolean(timeError) || undefined} {...register("Hour")}>
              <option value="">--</option>
              {HOURS.map((h) => (
                <option key={h.value} value={h.value}>
                  {h.label}
                </option>
              ))}
            </select>
          </div>
          <span aria-hidden="true" className="pb-2 font-bold">
            :
          </span>
          <div className="flex flex-col">
            <label htmlFor={`${id}Minute`} className="mb-1">
              Minute
            </label>
            <select id={`${id}Minute`} className={`nhs-input w-24 ${timeError ? "nhs-input-error" : ""}`} aria-invalid={Boolean(timeError) || undefined} {...register("Minute")}>
              <option value="">--</option>
              {MINUTES.map((m) => (
                <option key={m.value} value={m.value}>
                  {m.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </fieldset>
    </FormGroup>
  );
}
