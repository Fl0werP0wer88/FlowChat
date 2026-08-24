import { ShowGroupConversationCreatorButton } from '@/features/chat/navigation/show-group-conversation-creator-button';

import { GroupConversationList } from './group-conversation-list';

export function GroupConversationsManager() {
  return (
    <div className="grid gap-3">
      <ShowGroupConversationCreatorButton />

      <GroupConversationList />
    </div>
  );
}
