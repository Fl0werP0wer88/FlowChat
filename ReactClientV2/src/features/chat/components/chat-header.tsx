import type { ReactNode } from 'react';

import { BrandMark } from '@/components/brand/brand-mark';

interface ChatHeaderProps {
  actions?: ReactNode;
}

export function ChatHeader({ actions }: ChatHeaderProps) {
  return (
    <header className="flex min-h-16 items-center justify-between gap-4 border-b border-slate-200 px-4 py-3 sm:px-6">
      <div className="flex min-w-0 items-center gap-3 text-slate-950">
        <BrandMark className="size-8" />
        <div className="flex min-w-0 items-baseline gap-3">
          <span className="font-display text-lg font-bold tracking-[-0.025em]">FlowChat</span>
          <span className="hidden text-xs font-bold uppercase tracking-[0.16em] text-slate-400 sm:inline">
            Messages
          </span>
        </div>
      </div>

      {actions ? (
        <div className="flex min-w-0 items-center justify-end gap-2 sm:gap-3">{actions}</div>
      ) : null}
    </header>
  );
}
