import type { ReactNode } from 'react';

import { ShowConversationsButton } from '@/features/chat/navigation/show-conversations-button';

interface ConversationCreatorLayoutProps {
  children: ReactNode;
  disabled: boolean;
  title: string;
}

export function ConversationCreatorLayout({
  children,
  disabled,
  title,
}: ConversationCreatorLayoutProps) {
  return (
    <aside
      className="flex min-h-64 flex-col border-b border-slate-200 bg-slate-50/70 lg:min-h-0 lg:border-r lg:border-b-0"
      aria-labelledby="conversation-creator-heading"
    >
      <div className="border-b border-slate-200 px-5 py-5 sm:px-6">
        <ShowConversationsButton disabled={disabled} />
        <p className="m-0 text-[0.68rem] font-extrabold uppercase tracking-[0.18em] text-blue-700">
          New conversation
        </p>
        <h1
          className="mt-1 mb-0 font-display text-2xl font-semibold tracking-[-0.035em] text-slate-950"
          id="conversation-creator-heading"
        >
          {title}
        </h1>
      </div>

      {children}
    </aside>
  );
}
