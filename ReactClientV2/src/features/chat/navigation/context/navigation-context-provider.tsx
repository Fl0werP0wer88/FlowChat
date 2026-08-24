import { useMemo, useReducer, type ReactNode } from 'react';

import {
  NavigationContext,
  type NavigationContextValue,
  type NavigationSidebarState,
  type NavigationState,
} from './navigation-context';

interface NavigationContextProviderProps {
  children: ReactNode;
}

type NavigationAction =
  | {
      type: 'showConversationCreator';
      conversationType: Extract<
        NavigationSidebarState,
        { view: 'conversationCreator' }
      >['conversationType'];
    }
  | { type: 'showConversations' };

const initialNavigationState: NavigationState = {
  sidebar: { view: 'conversations' },
  mainWindow: { view: 'conversationSelection' },
};

function navigationReducer(state: NavigationState, action: NavigationAction): NavigationState {
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

export function NavigationContextProvider({ children }: NavigationContextProviderProps) {
  const [activeView, dispatch] = useReducer(navigationReducer, initialNavigationState);
  const value = useMemo<NavigationContextValue>(
    () => ({
      activeView,
      showConversationCreator: (conversationType) =>
        dispatch({ type: 'showConversationCreator', conversationType }),
      showConversations: () => dispatch({ type: 'showConversations' }),
    }),
    [activeView],
  );

  return <NavigationContext.Provider value={value}>{children}</NavigationContext.Provider>;
}
