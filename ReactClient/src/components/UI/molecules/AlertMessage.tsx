import type { AuthNotice } from "../../../types/auth";

interface AlertMessageProps {
  notice: AuthNotice | null;
}

export function AlertMessage({ notice }: AlertMessageProps) {
  if (!notice) {
    return null;
  }

  const variantClassName = notice.kind === "error" ? "alert alert-error" : "alert alert-info";
  return <p className={variantClassName}>{notice.message}</p>;
}
