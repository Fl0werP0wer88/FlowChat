import { ArrowLeft } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';

export function ShowConversationsButton({ disabled }: { disabled?: boolean }) {
  const { showConversations } = useChatContext();

  return (
    <Button
      className="-ml-3 mb-4 px-3"
      type="button"
      variant="ghost"
      disabled={disabled}
      onClick={showConversations}
    >
      <ArrowLeft className="size-4" aria-hidden="true" />
      Back
    </Button>
  );
}
