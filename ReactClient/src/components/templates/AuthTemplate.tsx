import type { PropsWithChildren } from "react";

interface AuthTemplateProps extends PropsWithChildren {
  title: string;
  subtitle: string;
}

export function AuthTemplate({ title, subtitle, children }: AuthTemplateProps) {
  return (
    <section className="card auth-card">
      <h1>{title}</h1>
      <p className="card-subtitle">{subtitle}</p>
      {children}
    </section>
  );
}
