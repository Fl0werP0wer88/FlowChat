import { DuetConversationList } from '@/features/chat/conversations/duet/components/duet-conversation-list';
import { ShowDuetConversationCreatorButton } from '@/features/chat/navigation/show-duet-conversation-creator-button';

export function DuetConversationsPanel() {
  return (
    <div className="grid gap-3">
      <ShowDuetConversationCreatorButton />

      <DuetConversationList />
    </div>
  );
}
