import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';

import { DuetConversationCreator } from './duet-conversation-creator';
import { GroupConversationCreator } from './group-conversation-creator';

export function ConversationCreatorSidebar() {
  const { activeView } = useNavigationContext();

  if (activeView.sidebar.view !== 'conversationCreator') return null;

  return activeView.sidebar.conversationType === 'duet' ? (
    <DuetConversationCreator />
  ) : (
    <GroupConversationCreator />
  );
}
