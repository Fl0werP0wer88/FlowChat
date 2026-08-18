import { createContext } from 'react';

export type ChatViewState =
  | { view: 'conversations' }
  | {
      view: 'conversationCreator';
      conversationType: 'duet' | 'group';
    };

export interface ChatContextValue {
  activeView: ChatViewState;
  showConversationCreator: (
    conversationType: Extract<ChatViewState, { view: 'conversationCreator' }>['conversationType'],
  ) => void;
  showConversations: () => void;
}

export const ChatContext = createContext<ChatContextValue | null>(null);
