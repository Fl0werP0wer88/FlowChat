import { UsersRound } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';

export function ShowGroupConversationCreatorButton() {
  const { showConversationCreator } = useNavigationContext();

  return (
    <Button className="w-full" type="button" onClick={() => showConversationCreator('group')}>
      <UsersRound className="size-4" aria-hidden="true" />
      New group
    </Button>
  );
}
