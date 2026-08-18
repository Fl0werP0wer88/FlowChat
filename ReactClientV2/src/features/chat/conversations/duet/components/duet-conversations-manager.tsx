import { UserPlus } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';

import { DuetConversationList } from './duet-conversation-list';

export function DuetConversationsManager() {
  const { showConversationCreator } = useChatContext();

  return (
    <div className="grid gap-3">
      <Button className="w-full" type="button" onClick={showConversationCreator}>
        <UserPlus className="size-4" aria-hidden="true" />
        New duet
      </Button>

      <DuetConversationList />
    </div>
  );
}
