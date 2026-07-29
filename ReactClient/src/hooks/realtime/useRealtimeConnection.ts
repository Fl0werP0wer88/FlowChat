import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect, useEffectEvent } from "react";
import { useAuthStore } from "../../store/authStore";
import { useRealtimeStore } from "../../store/realtimeStore";
import type {
  GroupConversationChangedEvent,
  GroupConversationParticipantsAddedEvent,
  GroupConversationParticipantsRemovedEvent,
  PresenceChangedEvent,
  ChatMessageReceivedEvent,
} from "../../types/realtime";
import { chatHubUrl } from "./config";

interface UseRealtimeConnectionOptions {
  onMessageReceived?: (payload: ChatMessageReceivedEvent) => void;
  onPresenceChanged?: (payload: PresenceChangedEvent) => void;
  onGroupConversationChanged?: (payload: GroupConversationChangedEvent) => void;
  onGroupConversationParticipantsAdded?: (payload: GroupConversationParticipantsAddedEvent) => void;
  onGroupConversationParticipantsRemoved?: (payload: GroupConversationParticipantsRemovedEvent) => void;
  onDuetConversationsListChanged?: () => void;
  onReconnected?: () => void;
}

function resolveErrorMessage(error: unknown): string | null {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return null;
}

export function useRealtimeConnection({
  onMessageReceived,
  onPresenceChanged,
  onGroupConversationChanged,
  onGroupConversationParticipantsAdded,
  onGroupConversationParticipantsRemoved,
  onDuetConversationsListChanged,
  onReconnected,
}: UseRealtimeConnectionOptions) {
  const accessToken = useAuthStore((s) => s.accessToken);
  const { setStatus, setLastError } = useRealtimeStore.getState();

  const handleMessageReceived = useEffectEvent((payload: ChatMessageReceivedEvent) => {
    onMessageReceived?.(payload);
  });

  const handlePresenceChanged = useEffectEvent((payload: PresenceChangedEvent) => {
    onPresenceChanged?.(payload);
  });

  const handleGroupConversationChanged = useEffectEvent((payload: GroupConversationChangedEvent) => {
    onGroupConversationChanged?.(payload);
  });

  const handleGroupConversationParticipantsAdded = useEffectEvent((payload: GroupConversationParticipantsAddedEvent) => {
    onGroupConversationParticipantsAdded?.(payload);
  });

  const handleGroupConversationParticipantsRemoved = useEffectEvent((payload: GroupConversationParticipantsRemovedEvent) => {
    onGroupConversationParticipantsRemoved?.(payload);
  });

  const handleDuetConversationsListChanged = useEffectEvent(() => {
    onDuetConversationsListChanged?.();
  });
  const handleReconnected = useEffectEvent(() => {
    onReconnected?.();
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
        // getState() ensures the factory always returns the latest token (e.g. after a silent refresh)
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("MessageReceived", (payload: ChatMessageReceivedEvent) => {
      if (!isDisposed) {
        handleMessageReceived(payload);
      }
    });

    connection.on("PresenceChanged", (payload: PresenceChangedEvent) => {
      if (!isDisposed) {
        handlePresenceChanged(payload);
      }
    });

    connection.on("GroupConversationChanged", (payload: GroupConversationChangedEvent) => {
      if (!isDisposed) {
        handleGroupConversationChanged(payload);
      }
    });

    connection.on("GroupConversationParticipantsAdded", (payload: GroupConversationParticipantsAddedEvent) => {
      if (!isDisposed) {
        handleGroupConversationParticipantsAdded(payload);
      }
    });

    connection.on("GroupConversationParticipantsRemoved", (payload: GroupConversationParticipantsRemovedEvent) => {
      if (!isDisposed) {
        handleGroupConversationParticipantsRemoved(payload);
      }
    });

    connection.on("DuetConversationsListChanged", () => {
      if (!isDisposed) {
        handleDuetConversationsListChanged();
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
      handleReconnected();
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
      connection.off("MessageReceived");
      connection.off("PresenceChanged");
      connection.off("GroupConversationChanged");
      connection.off("GroupConversationParticipantsAdded");
      connection.off("GroupConversationParticipantsRemoved");
      connection.off("DuetConversationsListChanged");
      void startPromise.finally(() => connection.stop().catch(() => undefined));
    };
  }, [accessToken]);

}
