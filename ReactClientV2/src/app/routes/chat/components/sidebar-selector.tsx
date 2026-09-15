import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';

import { DuetConversationCreatorSidebar } from './conversation-creator-sidebar/duet-conversation-creator-sidebar';
import { GroupConversationCreatorSidebar } from './conversation-creator-sidebar/group-conversation-creator-sidebar';
import { ConversationsSidebar } from './conversations-sidebar/conversations-sidebar';

export function SidebarSelector() {
  const { activeView } = useNavigationContext();

  if (activeView.sidebar.view === 'conversations') return <ConversationsSidebar />;

  return activeView.sidebar.conversationType === 'duet' ? (
    <DuetConversationCreatorSidebar />
  ) : (
    <GroupConversationCreatorSidebar />
  );
}
