import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect, useEffectEvent, useState } from "react";
import type {
  ContactPresenceStatusesEvent,
  PresenceChangedEvent,
  PresencePreferencesEvent,
  RealtimeChatMessage,
  RealtimeConnectionStatus,
} from "../types/realtime";
import { chatHubUrl } from "./config";

interface UseRealtimeConnectionOptions {
  accessToken: string | null;
  onReceiveContactPresenceStatuses?: (payload: ContactPresenceStatusesEvent) => void;
  onReceiveMessage?: (payload: RealtimeChatMessage) => void;
  onReceivePresencePreferences?: (payload: PresencePreferencesEvent) => void;
  onPresenceChanged?: (payload: PresenceChangedEvent) => void;
}

function resolveErrorMessage(error: unknown): string | null {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return null;
}

export function useRealtimeConnection({
  accessToken,
  onReceiveContactPresenceStatuses,
  onReceiveMessage,
  onReceivePresencePreferences,
  onPresenceChanged,
}: UseRealtimeConnectionOptions) {
  const [status, setStatus] = useState<RealtimeConnectionStatus>("idle");
  const [lastError, setLastError] = useState<string | null>(null);

  const handleReceiveMessage = useEffectEvent((payload: RealtimeChatMessage) => {
    onReceiveMessage?.(payload);
  });

  const handleReceiveContactPresenceStatuses = useEffectEvent((payload: ContactPresenceStatusesEvent) => {
    onReceiveContactPresenceStatuses?.(payload);
  });

  const handleReceivePresencePreferences = useEffectEvent((payload: PresencePreferencesEvent) => {
    onReceivePresencePreferences?.(payload);
  });

  const handlePresenceChanged = useEffectEvent((payload: PresenceChangedEvent) => {
    onPresenceChanged?.(payload);
  });

  useEffect(() => {
    if (!accessToken) {
      setStatus("idle");
      setLastError(null);
      return;
    }

    let isDisposed = false;
    let shouldStopAfterStart = false;
    const connection = new HubConnectionBuilder()
      .withUrl(chatHubUrl, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("ReceiveMessage", (payload: RealtimeChatMessage) => {
      if (!isDisposed) {
        handleReceiveMessage(payload);
      }
    });

    connection.on("ReceiveContactPresenceStatuses", (payload: ContactPresenceStatusesEvent) => {
      if (!isDisposed) {
        handleReceiveContactPresenceStatuses(payload);
      }
    });

    connection.on("ReceivePresencePreferences", (payload: PresencePreferencesEvent) => {
      if (!isDisposed) {
        handleReceivePresencePreferences(payload);
      }
    });

    connection.on("PresenceChanged", (payload: PresenceChangedEvent) => {
      if (!isDisposed) {
        handlePresenceChanged(payload);
      }
    });

    connection.onreconnecting((error) => {
      if (isDisposed) {
        return;
      }

      setStatus("reconnecting");
      setLastError(resolveErrorMessage(error));
    });

    connection.onreconnected(() => {
      if (isDisposed) {
        return;
      }

      setStatus("connected");
      setLastError(null);
    });

    connection.onclose((error) => {
      if (isDisposed) {
        return;
      }

      setStatus(error ? "error" : "disconnected");
      setLastError(resolveErrorMessage(error));
    });

    const startConnection = async () => {
      setStatus("connecting");
      setLastError(null);

      try {
        await connection.start();

        if (shouldStopAfterStart || isDisposed) {
          return;
        }

        setStatus("connected");
      } catch (error) {
        if (shouldStopAfterStart || isDisposed) {
          return;
        }

        setStatus("error");
        setLastError(resolveErrorMessage(error));
      }
    };

    const startPromise = startConnection();

    return () => {
      isDisposed = true;
      shouldStopAfterStart = true;
      connection.off("ReceiveMessage");
      connection.off("ReceiveContactPresenceStatuses");
      connection.off("ReceivePresencePreferences");
      connection.off("PresenceChanged");
      void startPromise.finally(() => connection.stop().catch(() => undefined));
    };
  }, [accessToken]);

  return {
    status,
    lastError,
  };
}
