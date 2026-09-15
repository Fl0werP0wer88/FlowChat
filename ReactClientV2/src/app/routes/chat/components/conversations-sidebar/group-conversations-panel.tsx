import { GroupConversationList } from '@/features/chat/conversations/group/components/group-conversation-list';
import { ShowGroupConversationCreatorButton } from '@/features/chat/navigation/show-group-conversation-creator-button';

export function GroupConversationsPanel() {
  return (
    <div className="grid gap-3">
      <ShowGroupConversationCreatorButton />

      <GroupConversationList />
    </div>
  );
}
