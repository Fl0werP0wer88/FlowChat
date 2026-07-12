import { forwardRef, type ButtonHTMLAttributes, type PropsWithChildren } from "react";

type ButtonVariant = "primary" | "secondary" | "link";

export interface ButtonProps extends PropsWithChildren<ButtonHTMLAttributes<HTMLButtonElement>> {
  variant?: ButtonVariant;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = "primary", className, children, ...buttonProps },
  ref,
) {
  const variantClassName = `ui-button ui-button--${variant}`;
  const mergedClassName = className ? `${variantClassName} ${className}` : variantClassName;

  return (
    <button ref={ref} {...buttonProps} className={mergedClassName}>
      {children}
    </button>
  );
});
