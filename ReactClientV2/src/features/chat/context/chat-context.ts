import { createContext } from 'react';

export type ChatSidebarView = 'conversations' | 'conversationCreator';

export interface ChatContextValue {
  activeSidebarView: ChatSidebarView;
  showConversationCreator: () => void;
  showConversations: () => void;
}

export const ChatContext = createContext<ChatContextValue | null>(null);
