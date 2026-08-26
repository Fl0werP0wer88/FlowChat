import { useState } from 'react';

import { SidebarLayout } from '@/components/layouts/sidebar-layout';
import { Button } from '@/components/ui/button';

import { DuetConversationsPanel } from './duet-conversations-panel';
import { GroupConversationsPanel } from './group-conversations-panel';

type ConversationListType = 'groups' | 'duets';

export function ConversationsSidebar() {
  const [activeList, setActiveList] = useState<ConversationListType>('duets');

  return (
    <SidebarLayout
      className="chat-conversation-panel"
      eyebrow="Inbox"
      headingId="conversations-heading"
      title="Conversations"
    >
      <div
        className="grid w-full grid-cols-2 divide-x divide-slate-200 border-b border-slate-200 bg-white"
        role="group"
        aria-label="Conversation type"
      >
        <Button
          className="min-h-12 w-full rounded-none px-3 text-base font-extrabold shadow-none hover:translate-y-0 focus-visible:z-10 focus-visible:ring-inset focus-visible:ring-offset-0"
          type="button"
          variant={activeList === 'groups' ? 'primary' : 'ghost'}
          aria-pressed={activeList === 'groups'}
          onClick={() => setActiveList('groups')}
        >
          Groups
        </Button>
        <Button
          className="min-h-12 w-full rounded-none px-3 text-base font-extrabold shadow-none hover:translate-y-0 focus-visible:z-10 focus-visible:ring-inset focus-visible:ring-offset-0"
          type="button"
          variant={activeList === 'duets' ? 'primary' : 'ghost'}
          aria-pressed={activeList === 'duets'}
          onClick={() => setActiveList('duets')}
        >
          Duets
        </Button>
      </div>

      <section
        className="min-h-0 flex-1 overflow-y-auto p-3 sm:p-4"
        aria-labelledby="conversations-heading"
      >
        {activeList === 'duets' ? <DuetConversationsPanel /> : <GroupConversationsPanel />}
      </section>
    </SidebarLayout>
  );
}
