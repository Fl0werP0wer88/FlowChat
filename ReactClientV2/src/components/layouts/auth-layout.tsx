import type { ReactNode } from 'react';

import { BrandMark } from '@/components/brand/brand-mark';

interface AuthLayoutProps {
  eyebrow: string;
  title: string;
  description: string;
  brandContent: ReactNode;
  children: ReactNode;
}

export function AuthLayout({
  eyebrow,
  title,
  description,
  brandContent,
  children,
}: AuthLayoutProps) {
  return (
    <main className="auth-stage">
      <div className="signal-field" aria-hidden="true">
        <span className="signal-line signal-line--one" />
        <span className="signal-line signal-line--two" />
        <span className="signal-node signal-node--one" />
        <span className="signal-node signal-node--two" />
        <span className="signal-node signal-node--three" />
      </div>

      <section className="brand-plane" aria-labelledby="flowchat-brand">
        <header className="brand-lockup">
          <BrandMark />
          <span id="flowchat-brand">FlowChat</span>
        </header>
        {brandContent}
        <p className="brand-footnote">Designed and developed by Piotr Kwiatkowski.</p>
      </section>

      <section className="auth-workspace" aria-labelledby="auth-title">
        <div className="auth-heading">
          <p className="auth-eyebrow">{eyebrow}</p>
          <h1 id="auth-title">{title}</h1>
          <p>{description}</p>
        </div>
        {children}
      </section>
    </main>
  );
}
