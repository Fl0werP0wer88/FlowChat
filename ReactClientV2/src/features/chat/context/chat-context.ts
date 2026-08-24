import { createContext } from 'react';

export type ChatSidebarState =
  | { view: 'conversations' }
  | {
      view: 'conversationCreator';
      conversationType: 'duet' | 'group';
    };

export type ChatMainWindowState = { view: 'conversationSelection' };

export interface ChatViewState {
  sidebar: ChatSidebarState;
  mainWindow: ChatMainWindowState;
}

export interface ChatContextValue {
  activeView: ChatViewState;
  showConversationCreator: (
    conversationType: Extract<
      ChatSidebarState,
      { view: 'conversationCreator' }
    >['conversationType'],
  ) => void;
  showConversations: () => void;
}

export const ChatContext = createContext<ChatContextValue | null>(null);
