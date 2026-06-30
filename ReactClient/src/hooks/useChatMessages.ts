import { useQueryClient } from "@tanstack/react-query";
import type { KeyboardEvent } from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useAuthStore } from "../store/authStore";
import type { Contact } from "../types/contacts";
import type { RealtimeChatMessage } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { getConversationMessages } from "../api/chatApi";
import type { ConversationCacheEntry } from "../features/conversations/duet/queries/conversationCache";
import {
  createMessage,
  mapConversationMessage,
  sortMessages,
} from "../features/conversations/duet/queries/conversationCache";
import { useConversationQuery } from "../features/conversations/duet/queries/useConversationQuery";
import { useSendMessageMutation } from "../features/conversations/duet/queries/useSendMessageMutation";

export function useChatMessages(activeContact: Contact | null) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();

  const [draft, setDraft] = useState("");
  const [sendError, setSendError] = useState<string | null>(null);
  const [olderMessagesError, setOlderMessagesError] = useState<string | null>(null);
  const [isLoadingOlderMessages, setIsLoadingOlderMessages] = useState(false);

  // Stores the callback supplied per-call to openContactConversation, fired once when query resolves
  const onConversationOpenedRef = useRef<((contactUserId: string, conversationId: string) => void) | null>(null);
  const lastNotifiedKeyRef = useRef<string | null>(null);
  const isLoadingOlderMessagesRef = useRef(false);

  const {
    data: conversationData,
    isLoading: isLoadingConversation,
    error: conversationQueryError,
  } = useConversationQuery(activeContact, accessToken, ownerUserId);

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
    onError: setSendError,
  });

  const loadOlderMessages = useCallback(async () => {
    if (!activeContact || !accessToken || isLoadingOlderMessagesRef.current) {
      return;
    }

    const current = queryClient.getQueryData<ConversationCacheEntry>(["conversation", activeContact.userId]);
    if (!current?.hasMore || !current.nextBeforeSentAtUtc || !current.nextBeforeMessageId) {
      return;
    }

    isLoadingOlderMessagesRef.current = true;
    setIsLoadingOlderMessages(true);
    setOlderMessagesError(null);

    try {
      const result = await getConversationMessages(
        current.conversationId,
        {
          beforeSentAtUtc: current.nextBeforeSentAtUtc,
          beforeMessageId: current.nextBeforeMessageId,
        },
        accessToken,
      );

      queryClient.setQueryData<ConversationCacheEntry>(
        ["conversation", activeContact.userId],
        (cached) => {
          if (!cached) {
            return cached;
          }

          const existingIds = new Set(cached.messages.map((message) => message.id));
          const olderMessages = result.messages
            .map((message) => mapConversationMessage(message, ownerUserId))
            .filter((message) => !existingIds.has(message.id));

          return {
            ...cached,
            messages: sortMessages([...olderMessages, ...cached.messages]),
            nextBeforeSentAtUtc: result.nextBeforeSentAtUtc,
            nextBeforeMessageId: result.nextBeforeMessageId,
            hasMore: result.hasMore,
          };
        },
      );
    } catch (error) {
      setOlderMessagesError(error instanceof Error ? error.message : "Nie udalo sie pobrac starszych wiadomosci.");
    } finally {
      isLoadingOlderMessagesRef.current = false;
      setIsLoadingOlderMessages(false);
    }
  }, [accessToken, activeContact, ownerUserId, queryClient]);

  const receiveRealtimeMessage = (payload: RealtimeChatMessage) => {
    if (!conversationData || payload.conversationId !== conversationData.conversationId) {
      return;
    }

    queryClient.setQueryData<ConversationCacheEntry>(
      ["conversation", activeContact?.userId],
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
          messages: sortMessages([
            ...current.messages,
            createMessage(
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
    setSendError(null);

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

  const openContactConversation = (
    _contact: Contact,
    onConversationOpened?: (contactUserId: string, conversationId: string) => void,
  ) => {
    onConversationOpenedRef.current = onConversationOpened ?? null;
    setSendError(null);
    setOlderMessagesError(null);
  };

  const conversationError =
    conversationQueryError instanceof Error ? conversationQueryError.message : null;

  return {
    messages: conversationData?.messages ?? [],
    draft,
    activeConversationId: conversationData?.conversationId ?? null,
    conversationError,
    isLoadingConversation,
    isSendingMessage: sendMessageMutation.isPending,
    sendError,
    hasOlderMessages: conversationData?.hasMore ?? false,
    isLoadingOlderMessages,
    olderMessagesError,
    setDraft,
    sendDraft,
    loadOlderMessages,
    handleDraftKeyDown,
    receiveRealtimeMessage,
    openContactConversation,
  };
}
