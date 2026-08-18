import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Suspense, type ReactNode } from 'react';
import { ErrorBoundary } from 'react-error-boundary';
import { Toaster } from 'sonner';

import { AuthBootstrap } from '@/components/auth/auth-bootstrap';
import { MainErrorFallback } from '@/components/errors/main-error-fallback';
import { LoadingScreen } from '@/components/ui/loading-screen';
import { queryClient } from '@/lib/query-client';

import { RealtimeBootstrap } from './realtime/realtime-bootstrap';

interface AppProviderProps {
  children: ReactNode;
}

export function AppProvider({ children }: AppProviderProps) {
  return (
    <Suspense fallback={<LoadingScreen label="Loading FlowChat" />}>
      <ErrorBoundary
        FallbackComponent={MainErrorFallback}
        onReset={() => window.location.assign('/')}
      >
        <QueryClientProvider client={queryClient}>
          {import.meta.env.DEV ? <ReactQueryDevtools initialIsOpen={false} /> : null}
          <Toaster richColors position="top-right" closeButton />
          <AuthBootstrap>
            <RealtimeBootstrap>{children}</RealtimeBootstrap>
          </AuthBootstrap>
        </QueryClientProvider>
      </ErrorBoundary>
    </Suspense>
  );
}
