import { useMemo, useState, type ReactNode } from 'react';

import { ChatContext, type ChatContextValue, type ChatSidebarView } from './chat-context';

interface ChatContextProviderProps {
  children: ReactNode;
}

export function ChatContextProvider({ children }: ChatContextProviderProps) {
  const [activeSidebarView, setActiveSidebarView] = useState<ChatSidebarView>('conversations');
  const value = useMemo<ChatContextValue>(
    () => ({
      activeSidebarView,
      showConversationCreator: () => setActiveSidebarView('conversationCreator'),
      showConversations: () => setActiveSidebarView('conversations'),
    }),
    [activeSidebarView],
  );

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
}
