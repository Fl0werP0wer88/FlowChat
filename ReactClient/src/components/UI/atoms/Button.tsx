import type { ButtonHTMLAttributes, PropsWithChildren } from "react";

type ButtonVariant = "primary" | "secondary" | "link";

export interface ButtonProps extends PropsWithChildren<ButtonHTMLAttributes<HTMLButtonElement>> {
  variant?: ButtonVariant;
}

export function Button({ variant = "primary", className, children, ...buttonProps }: ButtonProps) {
  const variantClassName = `ui-button ui-button--${variant}`;
  const mergedClassName = className ? `${variantClassName} ${className}` : variantClassName;

  return (
    <button {...buttonProps} className={mergedClassName}>
      {children}
    </button>
  );
}
