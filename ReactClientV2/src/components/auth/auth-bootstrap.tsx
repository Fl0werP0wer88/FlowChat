import { useEffect, type ReactNode } from 'react';

import { useSessionRefresher } from '@/hooks/use-session-refresher';
import { bootstrapSession } from '@/lib/auth-session';
import { useAuthStore } from '@/stores/auth-store';

import { LoadingScreen } from '../ui/loading-screen';

interface AuthBootstrapProps {
  children: ReactNode;
}

export function AuthBootstrap({ children }: AuthBootstrapProps) {
  const status = useAuthStore((state) => state.bootstrapStatus);
  useSessionRefresher();

  useEffect(() => {
    void bootstrapSession();
  }, []);

  if (status !== 'ready') return <LoadingScreen label="Restoring your session" />;
  return children;
}
