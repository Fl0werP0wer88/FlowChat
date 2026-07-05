import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import type { Contact } from "../types/contacts";
import type { RealtimeChatMessage } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { getDuetConversationMessages } from "../api/chatService";
import type { DuetConversationCacheEntry } from "./caches/duetConversationCache";
import {
  createDuetMessage,
  mapDuetConversationMessage,
  sortDuetMessages,
} from "./caches/duetConversationCache";
import { useSendMessageMutation } from "./mutations/useSendMessageMutation";
import { useDuetConversationQuery } from "./queries/useDuetConversationQuery";

export function useChatMessages(activeContact: Contact | null) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();

  const [isLoadingOlderMessages, setIsLoadingOlderMessages] = useState(false);

  // Stores the callback supplied per-call to openContactConversation, fired once when query resolves
  const onConversationOpenedRef = useRef<((contactUserId: string, conversationId: string) => void) | null>(null);
  const lastNotifiedKeyRef = useRef<string | null>(null);
  const isLoadingOlderMessagesRef = useRef(false);

  const {
    data: conversationData,
    isLoading: isLoadingConversation,
    error: conversationQueryError,
  } = useDuetConversationQuery(activeContact, accessToken, ownerUserId);

  useEffect(() => {
    if (!conversationData || !activeContact) {
      return;
    }

    const key = `${activeContact.userId}:${conversationData.conversationId}`;
    if (key === lastNotifiedKeyRef.current) {
      return;
    }

    lastNotifiedKeyRef.current = key;
    onConversationOpenedRef.current?.(activeContact.userId, conversationData.conversationId);
    onConversationOpenedRef.current = null;
  }, [conversationData, activeContact]);

  const sendMessageMutation = useSendMessageMutation({
    accessToken,
    activeContactUserId: activeContact?.userId,
    ownerUserId,
    userLogin,
    onError: (message) => toast.error(message),
  });

  const loadOlderMessages = useCallback(async () => {
    if (!activeContact || !accessToken || isLoadingOlderMessagesRef.current) {
      return;
    }

    const current = queryClient.getQueryData<DuetConversationCacheEntry>(["duetConversation", activeContact.userId]);
    if (!current?.hasMore || !current.nextBeforeSentAtUtc || !current.nextBeforeMessageId) {
      return;
    }

    isLoadingOlderMessagesRef.current = true;
    setIsLoadingOlderMessages(true);

    try {
      const result = await getDuetConversationMessages(
        current.conversationId,
        {
          beforeSentAtUtc: current.nextBeforeSentAtUtc,
          beforeMessageId: current.nextBeforeMessageId,
        },
        accessToken,
      );

      queryClient.setQueryData<DuetConversationCacheEntry>(
        ["duetConversation", activeContact.userId],
        (cached) => {
          if (!cached) {
            return cached;
          }

          const existingIds = new Set(cached.messages.map((message) => message.id));
          const olderMessages = result.messages
            .map((message) => mapDuetConversationMessage(message, ownerUserId))
            .filter((message) => !existingIds.has(message.id));

          return {
            ...cached,
            messages: sortDuetMessages([...olderMessages, ...cached.messages]),
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
  }, [accessToken, activeContact, ownerUserId, queryClient]);

  const receiveRealtimeMessage = (payload: RealtimeChatMessage) => {
    if (!conversationData || payload.conversationId !== conversationData.conversationId) {
      return;
    }

    queryClient.setQueryData<DuetConversationCacheEntry>(
      ["duetConversation", activeContact?.userId],
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
          messages: sortDuetMessages([
            ...current.messages,
            createDuetMessage(
              sender,
              payload.text,
              payload.sentAtUtc,
              payload.messageId,
              payload.conversationId,
              payload.senderUserId,
              payload.senderDisplayName,
              payload.sequenceNum,
            ),
          ]),
        };
      },
    );
  };

  const sendDraft = async (draft: string): Promise<boolean> => {
    const text = draft.trim();
    const conversationId = conversationData?.conversationId;

    if (!text || !conversationId || !accessToken || !ownerUserId || sendMessageMutation.isPending) {
      return false;
    }

    const messageId = crypto.randomUUID();

    try {
      await sendMessageMutation.mutateAsync({ messageId, conversationId, text, senderDisplayName: userLogin });
      return true;
    } catch {
      // error is handled in onError
      return false;
    }
  };

  const openContactConversation = (
    _contact: Contact,
    onConversationOpened?: (contactUserId: string, conversationId: string) => void,
  ) => {
    onConversationOpenedRef.current = onConversationOpened ?? null;
  };

  return {
    messages: conversationData?.messages ?? [],
    activeConversationId: conversationData?.conversationId ?? null,
    hasConversationError: conversationQueryError !== null,
    isLoadingConversation,
    isSendingMessage: sendMessageMutation.isPending,
    hasOlderMessages: conversationData?.hasMore ?? false,
    isLoadingOlderMessages,
    sendDraft,
    loadOlderMessages,
    receiveRealtimeMessage,
    openContactConversation,
  };
}
