import type { ReactNode } from 'react';

interface AppLayoutProps {
  header: ReactNode;
  sidebar: ReactNode;
  contentLabel: string;
  children: ReactNode;
}

export function AppLayout({ header, sidebar, contentLabel, children }: AppLayoutProps) {
  return (
    <main className="app-shell min-h-svh bg-slate-100 lg:p-4">
      <div className="mx-auto grid min-h-svh max-w-[100rem] grid-rows-[auto_1fr] overflow-hidden bg-white lg:h-[calc(100svh-2rem)] lg:min-h-0 lg:rounded-[1.75rem] lg:border lg:border-slate-200 lg:shadow-[0_24px_80px_-56px_rgba(15,35,75,0.65)]">
        {header}

        <div className="grid min-h-0 lg:grid-cols-[21rem_minmax(0,1fr)]">
          {sidebar}

          <section
            className="app-content-panel min-h-[32rem] min-w-0 bg-white lg:min-h-0 lg:overflow-y-auto"
            aria-label={contentLabel}
          >
            {children}
          </section>
        </div>
      </div>
    </main>
  );
}
