import { UserPlus } from 'lucide-react';

import { Button } from '@/components/ui/button';

import { DuetConversationList } from './duet-conversation-list';

interface DuetConversationsManagerProps {
  onCreateConversation: () => void;
}

export function DuetConversationsManager({ onCreateConversation }: DuetConversationsManagerProps) {
  return (
    <div className="grid gap-3">
      <Button className="w-full" type="button" onClick={onCreateConversation}>
        <UserPlus className="size-4" aria-hidden="true" />
        New duet
      </Button>

      <DuetConversationList />
    </div>
  );
}
