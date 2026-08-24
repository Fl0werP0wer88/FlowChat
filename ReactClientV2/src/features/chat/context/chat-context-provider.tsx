import { useMemo, useReducer, type ReactNode } from 'react';

import {
  ChatContext,
  type ChatContextValue,
  type ChatSidebarState,
  type ChatViewState,
} from './chat-context';

interface ChatContextProviderProps {
  children: ReactNode;
}

type ChatViewAction =
  | {
      type: 'showConversationCreator';
      conversationType: Extract<
        ChatSidebarState,
        { view: 'conversationCreator' }
      >['conversationType'];
    }
  | { type: 'showConversations' };

const initialChatViewState: ChatViewState = {
  sidebar: { view: 'conversations' },
  mainWindow: { view: 'conversationSelection' },
};

function chatViewReducer(state: ChatViewState, action: ChatViewAction): ChatViewState {
  switch (action.type) {
    case 'showConversationCreator':
      return {
        ...state,
        sidebar: {
          view: 'conversationCreator',
          conversationType: action.conversationType,
        },
      };
    case 'showConversations':
      return { ...state, sidebar: { view: 'conversations' } };
  }
}

export function ChatContextProvider({ children }: ChatContextProviderProps) {
  const [activeView, dispatch] = useReducer(chatViewReducer, initialChatViewState);
  const value = useMemo<ChatContextValue>(
    () => ({
      activeView,
      showConversationCreator: (conversationType) =>
        dispatch({ type: 'showConversationCreator', conversationType }),
      showConversations: () => dispatch({ type: 'showConversations' }),
    }),
    [activeView],
  );

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
}
