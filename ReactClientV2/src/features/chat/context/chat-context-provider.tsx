import { useMemo, useState, type ReactNode } from 'react';

import { ChatContext, type ChatContextValue, type ChatViewState } from './chat-context';

interface ChatContextProviderProps {
  children: ReactNode;
}

export function ChatContextProvider({ children }: ChatContextProviderProps) {
  const [activeView, setActiveView] = useState<ChatViewState>({ view: 'conversations' });
  const value = useMemo<ChatContextValue>(
    () => ({
      activeView,
      showConversationCreator: (conversationType) =>
        setActiveView({ view: 'conversationCreator', conversationType }),
      showConversations: () => setActiveView({ view: 'conversations' }),
    }),
    [activeView],
  );

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
}
