import type { InputHTMLAttributes } from "react";

export type InputProps = InputHTMLAttributes<HTMLInputElement>;

export function Input({ className, ...inputProps }: InputProps) {
  const mergedClassName = className ? `ui-input ${className}` : "ui-input";
  return <input {...inputProps} className={mergedClassName} />;
}
