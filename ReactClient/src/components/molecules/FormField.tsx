import type { PropsWithChildren } from "react";

export interface FormFieldProps extends PropsWithChildren {
  label: string;
  htmlFor: string;
  hint?: string;
}

export function FormField({ label, htmlFor, hint, children }: FormFieldProps) {
  return (
    <div className="form-field">
      <label htmlFor={htmlFor} className="form-label">
        {label}
      </label>
      {children}
      {hint ? <p className="form-hint">{hint}</p> : null}
    </div>
  );
}
