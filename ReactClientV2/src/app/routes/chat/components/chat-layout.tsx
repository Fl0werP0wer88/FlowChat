import type { ReactNode } from 'react';

import { BrandMark } from '@/components/brand/brand-mark';

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
          <aside className="chat-conversation-panel flex min-h-64 flex-col border-b border-slate-200 bg-slate-50/70 lg:min-h-0 lg:border-r lg:border-b-0">
            <div className="border-b border-slate-200 px-5 py-5 sm:px-6">
              <p className="m-0 text-[0.68rem] font-extrabold uppercase tracking-[0.18em] text-blue-700">
                Inbox
              </p>
              <h1 className="mt-1 mb-0 font-display text-2xl font-semibold tracking-[-0.035em] text-slate-950">
                Conversations
              </h1>
            </div>

            <nav
              className="min-h-0 flex-1 overflow-y-auto p-3 sm:p-4"
              aria-label="Conversation list"
            >
              {conversationList}
            </nav>
          </aside>

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
