import { AlertTriangle } from 'lucide-react';
import type { FallbackProps } from 'react-error-boundary';

import { Button } from '@/components/ui/button';

export function MainErrorFallback({ resetErrorBoundary }: FallbackProps) {
  return (
    <main className="grid min-h-svh place-items-center bg-slate-50 px-6">
      <section className="grid max-w-md justify-items-start gap-5">
        <span className="grid size-12 place-items-center rounded-full bg-rose-100 text-rose-700">
          <AlertTriangle aria-hidden="true" />
        </span>
        <div className="grid gap-2">
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-rose-700">
            Application error
          </p>
          <h1 className="font-display text-3xl font-semibold tracking-tight text-slate-950">
            FlowChat hit an unexpected problem.
          </h1>
          <p className="leading-7 text-slate-600">
            Try reloading this view. Your account data has not been changed.
          </p>
        </div>
        <Button type="button" onClick={resetErrorBoundary}>
          Try again
        </Button>
      </section>
    </main>
  );
}
