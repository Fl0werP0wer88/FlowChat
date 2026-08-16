import type { ReactNode } from 'react';

import { AboutCarousel } from '@/components/about/about-carousel';
import { BrandMark } from '@/components/brand/brand-mark';

interface AuthLayoutProps {
  eyebrow: string;
  title: string;
  description: string;
  children: ReactNode;
}

export function AuthLayout({ eyebrow, title, description, children }: AuthLayoutProps) {
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
        <div className="brand-lockup">
          <BrandMark />
          <span id="flowchat-brand">FlowChat</span>
        </div>
        <AboutCarousel />
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
