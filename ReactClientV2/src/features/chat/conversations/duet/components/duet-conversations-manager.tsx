import { ShowDuetConversationCreatorButton } from '@/features/chat/navigation/show-duet-conversation-creator-button';

import { DuetConversationList } from './duet-conversation-list';

export function DuetConversationsManager() {
  return (
    <div className="grid gap-3">
      <ShowDuetConversationCreatorButton />

      <DuetConversationList />
    </div>
  );
}
