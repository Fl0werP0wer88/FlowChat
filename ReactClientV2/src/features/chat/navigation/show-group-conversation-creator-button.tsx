import { UsersRound } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';

export function ShowGroupConversationCreatorButton() {
  const { showConversationCreator } = useChatContext();

  return (
    <Button className="w-full" type="button" onClick={() => showConversationCreator('group')}>
      <UsersRound className="size-4" aria-hidden="true" />
      New group
    </Button>
  );
}
