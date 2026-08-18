import { UsersRound } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';

import { GroupConversationList } from './group-conversation-list';

export function GroupConversationsManager() {
  const { showConversationCreator } = useChatContext();

  return (
    <div className="grid gap-3">
      <Button className="w-full" type="button" onClick={() => showConversationCreator('group')}>
        <UsersRound className="size-4" aria-hidden="true" />
        New group
      </Button>

      <GroupConversationList />
    </div>
  );
}
