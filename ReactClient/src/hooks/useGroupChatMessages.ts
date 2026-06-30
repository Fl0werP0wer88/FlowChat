import { useQueryClient } from "@tanstack/react-query";
import type { KeyboardEvent } from "react";
import { useCallback, useRef, useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import type { RealtimeChatMessage } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import type { GroupConversation } from "../api/chatApi";
import { getGroupConversationMessages } from "../api/chatApi";
import type { GroupConversationCacheEntry } from "./caches/groupConversationCache";
import {
  createGroupMessage,
  mapGroupConversationMessage,
  sortGroupMessages,
} from "./caches/groupConversationCache";
import { useSendGroupMessageMutation } from "./mutations/useSendGroupMessageMutation";
import { useGroupConversationQuery } from "./queries/useGroupConversationQuery";

export function useGroupChatMessages(activeGroupConversation: GroupConversation | null) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();

  const [draft, setDraft] = useState("");
  const [isLoadingOlderMessages, setIsLoadingOlderMessages] = useState(false);

  const isLoadingOlderMessagesRef = useRef(false);

  const {
    data: conversationData,
    isLoading: isLoadingConversation,
    error: conversationQueryError,
  } = useGroupConversationQuery(activeGroupConversation, accessToken, ownerUserId);

  const sendMessageMutation = useSendGroupMessageMutation({
    accessToken,
    activeGroupConversationId: activeGroupConversation?.conversationId,
    ownerUserId,
    userLogin,
    onError: (message) => toast.error(message),
  });

  const loadOlderMessages = useCallback(async () => {
    if (!activeGroupConversation || !accessToken || isLoadingOlderMessagesRef.current) {
      return;
    }

    const current = queryClient.getQueryData<GroupConversationCacheEntry>([
      "groupConversation",
      activeGroupConversation.conversationId,
    ]);
    if (!current?.hasMore || !current.nextBeforeSentAtUtc || !current.nextBeforeMessageId) {
      return;
    }

    isLoadingOlderMessagesRef.current = true;
    setIsLoadingOlderMessages(true);

    try {
      const result = await getGroupConversationMessages(
        current.conversationId,
        {
          beforeSentAtUtc: current.nextBeforeSentAtUtc,
          beforeMessageId: current.nextBeforeMessageId,
        },
        accessToken,
      );

      queryClient.setQueryData<GroupConversationCacheEntry>(
        ["groupConversation", activeGroupConversation.conversationId],
        (cached) => {
          if (!cached) {
            return cached;
          }

          const existingIds = new Set(cached.messages.map((message) => message.id));
          const olderMessages = result.messages
            .map((message) => mapGroupConversationMessage(message, ownerUserId))
            .filter((message) => !existingIds.has(message.id));

          return {
            ...cached,
            messages: sortGroupMessages([...olderMessages, ...cached.messages]),
            nextBeforeSentAtUtc: result.nextBeforeSentAtUtc,
            nextBeforeMessageId: result.nextBeforeMessageId,
            hasMore: result.hasMore,
          };
        },
      );
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Nie udalo sie pobrac starszych wiadomosci.");
    } finally {
      isLoadingOlderMessagesRef.current = false;
      setIsLoadingOlderMessages(false);
    }
  }, [accessToken, activeGroupConversation, ownerUserId, queryClient]);

  const receiveRealtimeMessage = (payload: RealtimeChatMessage) => {
    if (!conversationData || payload.conversationId !== conversationData.conversationId) {
      return;
    }

    queryClient.setQueryData<GroupConversationCacheEntry>(
      ["groupConversation", activeGroupConversation?.conversationId],
      (current) => {
        if (!current) {
          return current;
        }

        if (current.messages.some((m) => m.id === payload.messageId)) {
          return current;
        }

        const sender = ownerUserId && payload.senderUserId === ownerUserId ? "me" : "other";
        return {
          ...current,
          messages: sortGroupMessages([
            ...current.messages,
            createGroupMessage(
              sender,
              payload.text,
              payload.sentAtUtc,
              payload.messageId,
              payload.conversationId,
              payload.senderUserId,
              payload.senderDisplayName,
            ),
          ]),
        };
      },
    );
  };

  const sendDraft = async () => {
    const text = draft.trim();
    const conversationId = conversationData?.conversationId;

    if (!text || !conversationId || !accessToken || !ownerUserId || sendMessageMutation.isPending) {
      return;
    }

    const messageId = crypto.randomUUID();

    try {
      await sendMessageMutation.mutateAsync({ messageId, conversationId, text, senderDisplayName: userLogin });
      setDraft("");
    } catch {
      // error is handled in onError
    }
  };

  const handleDraftKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void sendDraft();
    }
  };

  const openGroupConversation = () => {};

  return {
    messages: conversationData?.messages ?? [],
    draft,
    activeConversationId: conversationData?.conversationId ?? null,
    activeConversationName: conversationData?.name ?? activeGroupConversation?.name ?? null,
    hasConversationError: conversationQueryError !== null,
    isLoadingConversation,
    isSendingMessage: sendMessageMutation.isPending,
    hasOlderMessages: conversationData?.hasMore ?? false,
    isLoadingOlderMessages,
    setDraft,
    sendDraft,
    loadOlderMessages,
    handleDraftKeyDown,
    receiveRealtimeMessage,
    openGroupConversation,
  };
}
