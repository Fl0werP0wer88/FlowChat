import type { TextareaHTMLAttributes } from "react";

export type TextAreaProps = TextareaHTMLAttributes<HTMLTextAreaElement>;

export function TextArea({ className, ...textAreaProps }: TextAreaProps) {
  const mergedClassName = className ? `ui-textarea ${className}` : "ui-textarea";
  return <textarea {...textAreaProps} className={mergedClassName} />;
}
