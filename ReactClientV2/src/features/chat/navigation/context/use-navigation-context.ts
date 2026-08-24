import { useContext } from 'react';

import { NavigationContext } from './navigation-context';

export function useNavigationContext() {
  const context = useContext(NavigationContext);

  if (!context) {
    throw new Error('useNavigationContext must be used within NavigationContextProvider.');
  }

  return context;
}
