import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';

import { env } from '@/config/env';
import { getAuthState } from '@/stores/auth-store';

type RealtimeEventHandler = (payload: unknown) => void;
type ReconnectedHandler = () => void;

export interface RealtimeClient {
  start: () => Promise<void>;
  stop: () => void;
  subscribe: (eventName: string, handler: RealtimeEventHandler) => () => void;
  subscribeToReconnected: (handler: ReconnectedHandler) => () => void;
}

const initialRetryDelayMs = 5_000;
const strictModeStopDelayMs = 0;

export function resolveRealtimeAccessToken(): string {
  return getAuthState().session?.accessToken ?? '';
}

export function createRealtimeClient(
  connection: HubConnection,
  retryDelayMs = initialRetryDelayMs,
): RealtimeClient {
  let shouldBeConnected = false;
  let startPromise: Promise<void> | null = null;
  let stopPromise: Promise<void> | null = null;
  let retryTimer: ReturnType<typeof setTimeout> | null = null;
  let stopTimer: ReturnType<typeof setTimeout> | null = null;
  const reconnectedHandlers = new Set<ReconnectedHandler>();

  const clearRetry = () => {
    if (retryTimer !== null) {
      clearTimeout(retryTimer);
      retryTimer = null;
    }
  };

  const scheduleRetry = () => {
    if (!shouldBeConnected || retryTimer !== null) return;

    retryTimer = setTimeout(() => {
      retryTimer = null;
      void ensureStarted();
    }, retryDelayMs);
  };

  const ensureStarted = async (): Promise<void> => {
    if (!shouldBeConnected) return;

    if (stopPromise) await stopPromise;
    if (!shouldBeConnected || connection.state !== HubConnectionState.Disconnected) return;
    if (startPromise) return startPromise;

    const currentStart = connection
      .start()
      .catch(() => {
        scheduleRetry();
      })
      .finally(() => {
        if (startPromise === currentStart) startPromise = null;
      });

    startPromise = currentStart;
    return currentStart;
  };

  const performStop = async () => {
    if (startPromise) await startPromise;
    if (shouldBeConnected || connection.state === HubConnectionState.Disconnected) return;
    if (stopPromise) return stopPromise;

    const currentStop = connection
      .stop()
      .catch(() => undefined)
      .finally(() => {
        if (stopPromise === currentStop) stopPromise = null;
      });

    stopPromise = currentStop;
    return currentStop;
  };

  connection.onreconnected(() => {
    for (const handler of reconnectedHandlers) handler();
  });

  connection.onclose(() => {
    scheduleRetry();
  });

  return {
    start: () => {
      shouldBeConnected = true;
      clearRetry();

      if (stopTimer !== null) {
        clearTimeout(stopTimer);
        stopTimer = null;
      }

      return ensureStarted();
    },
    stop: () => {
      shouldBeConnected = false;
      clearRetry();
      if (stopTimer !== null) return;

      // Deferring stop lets the Strict Mode setup that immediately follows cleanup reuse the same connection
      stopTimer = setTimeout(() => {
        stopTimer = null;
        void performStop();
      }, strictModeStopDelayMs);
    },
    subscribe: (eventName, handler) => {
      connection.on(eventName, handler);
      return () => connection.off(eventName, handler);
    },
    subscribeToReconnected: (handler) => {
      reconnectedHandlers.add(handler);
      return () => reconnectedHandlers.delete(handler);
    },
  };
}

const connection = new HubConnectionBuilder()
  .withUrl(`${env.gatewayApiUrl}/hubs/chat`, {
    accessTokenFactory: resolveRealtimeAccessToken,
  })
  .withAutomaticReconnect()
  .configureLogging(LogLevel.None)
  .build();

const realtimeClient = createRealtimeClient(connection);

export const startRealtimeConnection = () => realtimeClient.start();
export const stopRealtimeConnection = () => realtimeClient.stop();
export const subscribeToRealtimeEvent = (eventName: string, handler: RealtimeEventHandler) =>
  realtimeClient.subscribe(eventName, handler);
export const subscribeToRealtimeReconnected = (handler: ReconnectedHandler) =>
  realtimeClient.subscribeToReconnected(handler);
