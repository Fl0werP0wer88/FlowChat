import { useState } from 'react';

import { Button } from '@/components/ui/button';

import { DuetConversationList } from '../duet/components/duet-conversation-list';
import { GroupConversationList } from '../group/components/group-conversation-list';

type ConversationListType = 'groups' | 'duets';

export function Conversations() {
  const [activeList, setActiveList] = useState<ConversationListType>('duets');

  return (
    <aside className="chat-conversation-panel flex min-h-64 flex-col border-b border-slate-200 bg-slate-50/70 lg:min-h-0 lg:border-r lg:border-b-0">
      <div className="border-b border-slate-200 px-5 py-5 sm:px-6">
        <p className="m-0 text-[0.68rem] font-extrabold uppercase tracking-[0.18em] text-blue-700">
          Inbox
        </p>
        <h1
          className="mt-1 mb-0 font-display text-2xl font-semibold tracking-[-0.035em] text-slate-950"
          id="conversations-heading"
        >
          Conversations
        </h1>
      </div>

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
        {activeList === 'duets' ? <DuetConversationList /> : <GroupConversationList />}
      </section>
    </aside>
  );
}
