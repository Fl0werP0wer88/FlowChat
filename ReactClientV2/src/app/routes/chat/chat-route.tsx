import { MessageCircleMore } from 'lucide-react';
import { useState } from 'react';

import { AppHeader } from '@/components/layouts/app-header';
import { AppLayout } from '@/components/layouts/app-layout';
import { ConversationCreator } from '@/features/chat/conversation-creator/components/conversation-creator';
import { Conversations } from '@/features/chat/conversations/components/conversations';
import { useDocumentTitle } from '@/hooks/use-document-title';

type SidebarView = 'conversations' | 'conversationCreator';

export function Component() {
  const [sidebarView, setSidebarView] = useState<SidebarView>('conversations');

  useDocumentTitle('Chat');

  return (
    <AppLayout
      contentLabel="Active conversation"
      header={<AppHeader />}
      sidebar={
        sidebarView === 'conversations' ? (
          <Conversations onCreateDuet={() => setSidebarView('conversationCreator')} />
        ) : (
          <ConversationCreator onBack={() => setSidebarView('conversations')} />
        )
      }
    >
      <div className="grid h-full min-h-[32rem] place-items-center px-6 py-16 text-center">
        <div className="grid max-w-md justify-items-center gap-5">
          <span className="grid size-16 place-items-center rounded-full bg-blue-50 text-blue-700 ring-8 ring-blue-50/60">
            <MessageCircleMore className="size-7" aria-hidden="true" />
          </span>
          <div className="grid gap-3">
            <p className="m-0 text-xs font-extrabold uppercase tracking-[0.18em] text-blue-700">
              Active conversation
            </p>
            <h2 className="m-0 font-display text-3xl font-semibold tracking-[-0.04em] text-slate-950 sm:text-4xl">
              Choose a conversation
            </h2>
            <p className="m-0 text-base leading-7 text-slate-600">
              Select a conversation from the list to open its messages here.
            </p>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
