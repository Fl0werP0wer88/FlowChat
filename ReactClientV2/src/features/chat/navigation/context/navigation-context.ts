import { createContext } from 'react';

export type NavigationSidebarState =
  | { view: 'conversations' }
  | {
      view: 'conversationCreator';
      conversationType: 'duet' | 'group';
    };

export type NavigationMainWindowState = { view: 'conversationSelection' };

export interface NavigationState {
  sidebar: NavigationSidebarState;
  mainWindow: NavigationMainWindowState;
}

export interface NavigationContextValue {
  activeView: NavigationState;
  showConversationCreator: (
    conversationType: Extract<
      NavigationSidebarState,
      { view: 'conversationCreator' }
    >['conversationType'],
  ) => void;
  showConversations: () => void;
}

export const NavigationContext = createContext<NavigationContextValue | null>(null);
