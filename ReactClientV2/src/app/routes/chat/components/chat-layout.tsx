import type { ReactNode } from 'react';

import { BrandMark } from '@/components/brand/brand-mark';
import { Conversations } from '@/features/chat/conversations/components/conversations';

interface ChatLayoutProps {
  conversationList: ReactNode;
  activeConversation: ReactNode;
  headerActions?: ReactNode;
}

export function ChatLayout({
  conversationList,
  activeConversation,
  headerActions,
}: ChatLayoutProps) {
  return (
    <main className="chat-shell min-h-svh bg-slate-100 lg:p-4">
      <div className="mx-auto grid min-h-svh max-w-[100rem] grid-rows-[auto_1fr] overflow-hidden bg-white lg:h-[calc(100svh-2rem)] lg:min-h-0 lg:rounded-[1.75rem] lg:border lg:border-slate-200 lg:shadow-[0_24px_80px_-56px_rgba(15,35,75,0.65)]">
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

          {headerActions ? (
            <div className="flex min-w-0 items-center justify-end gap-2 sm:gap-3">
              {headerActions}
            </div>
          ) : null}
        </header>

        <div className="grid min-h-0 lg:grid-cols-[21rem_minmax(0,1fr)]">
          <Conversations>{conversationList}</Conversations>

          <section
            className="chat-active-panel min-h-[32rem] min-w-0 bg-white lg:min-h-0 lg:overflow-y-auto"
            aria-label="Active conversation"
          >
            {activeConversation}
          </section>
        </div>
      </div>
    </main>
  );
}
