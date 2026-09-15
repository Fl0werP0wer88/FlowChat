import type { ReactNode } from 'react';

import { cn } from '@/utils/cn';

interface SidebarLayoutProps {
  backAction?: ReactNode;
  children: ReactNode;
  className?: string;
  eyebrow: string;
  headingId: string;
  title: string;
}

export function SidebarLayout({
  backAction,
  children,
  className,
  eyebrow,
  headingId,
  title,
}: SidebarLayoutProps) {
  return (
    <aside
      className={cn(
        'flex min-h-64 flex-col border-b border-slate-200 bg-slate-50/70 lg:min-h-0 lg:border-r lg:border-b-0',
        className,
      )}
      aria-labelledby={headingId}
    >
      <div className="border-b border-slate-200 px-5 py-5 sm:px-6">
        {backAction}
        <p className="m-0 text-[0.68rem] font-extrabold uppercase tracking-[0.18em] text-blue-700">
          {eyebrow}
        </p>
        <h1
          className="mt-1 mb-0 font-display text-2xl font-semibold tracking-[-0.035em] text-slate-950"
          id={headingId}
        >
          {title}
        </h1>
      </div>

      {children}
    </aside>
  );
}
