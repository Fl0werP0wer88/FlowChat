import { DuetConversationList } from '../duet/components/duet-conversation-list';
import { GroupConversationList } from '../group/components/group-conversation-list';

export function Conversations() {
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

      <section
        className="min-h-0 flex-1 overflow-y-auto p-3 sm:p-4"
        aria-labelledby="conversations-heading"
      >
        <DuetConversationList />
        <GroupConversationList />
      </section>
    </aside>
  );
}
