import { UserPlus } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';

export function ShowDuetConversationCreatorButton() {
  const { showConversationCreator } = useNavigationContext();

  return (
    <Button className="w-full" type="button" onClick={() => showConversationCreator('duet')}>
      <UserPlus className="size-4" aria-hidden="true" />
      New duet
    </Button>
  );
}
