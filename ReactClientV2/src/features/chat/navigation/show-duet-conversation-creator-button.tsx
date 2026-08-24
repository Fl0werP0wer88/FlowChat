import { UserPlus } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';

export function ShowDuetConversationCreatorButton() {
  const { showConversationCreator } = useChatContext();

  return (
    <Button className="w-full" type="button" onClick={() => showConversationCreator('duet')}>
      <UserPlus className="size-4" aria-hidden="true" />
      New duet
    </Button>
  );
}
