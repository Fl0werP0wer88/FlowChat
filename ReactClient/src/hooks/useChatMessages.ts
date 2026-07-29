import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import type { Contact } from "../types/contacts";
import type { ChatMessageReceivedEvent } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { getDuetConversationMessages, markConversationAsRead } from "../api/chatService";
import { calculateUnreadCount } from "../utils/chatUtils";
import type { DuetConversationCacheEntry } from "./caches/duetConversationCache";
import {
  createDuetMessage,
  mapDuetConversationMessage,
  sortDuetMessages,
} from "./caches/duetConversationCache";
import { useSendMessageMutation } from "./mutations/useSendMessageMutation";
import { useDuetConversationQuery } from "./queries/useDuetConversationQuery";
import { mergeSequencedMessage } from "./caches/sequencedMessageCache";
import { useConversationMessageSync } from "./useConversationMessageSync";

export function useChatMessages(activeContact: Contact | null) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
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
  const conversationQueryKey = useMemo(
    () => ["duetConversation", activeContact?.userId] as const,
    [activeContact?.userId],
  );
  const mapSynchronizedMessage = useCallback(
    (message: Parameters<typeof mapDuetConversationMessage>[0]) =>
      mapDuetConversationMessage(message, ownerUserId),
    [ownerUserId],
  );
  const synchronizeMessages = useConversationMessageSync<DuetConversationCacheEntry>({
    conversationId: conversationData?.conversationId ?? null,
    queryKey: conversationQueryKey,
    accessToken,
    mapMessage: mapSynchronizedMessage,
  });

  const markActiveDuetConversationAsRead = useCallback(async (
    contact?: Contact,
    conversationId?: string,
    sequenceNum?: number,
  ) => {
    const targetContact = contact ?? activeContact;
    const targetConversationId = conversationId ?? conversationData?.conversationId;

    if (!targetContact || !targetConversationId || !accessToken) {
      return;
    }
    const contiguousSequenceNum = sequenceNum
      ?? queryClient.getQueryData<DuetConversationCacheEntry>(conversationQueryKey)
        ?.lastContiguousSequenceNum
      ?? 0;

    queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
      current.map((item) => {
        if (item.userId !== targetContact.userId) {
          return item;
        }

        return {
          ...item,
          conversationId: targetConversationId,
          lastReadMsgSeqNum: Math.max(item.lastReadMsgSeqNum, contiguousSequenceNum),
          unreadCount: calculateUnreadCount(
            item.currentMsgSeqNum,
            Math.max(item.lastReadMsgSeqNum, contiguousSequenceNum),
          ),
        };
      }),
    );

    try {
      await markConversationAsRead(
        targetConversationId,
        contiguousSequenceNum,
        accessToken,
      );
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Nie udalo sie oznaczyc rozmowy jako przeczytanej.");
      void queryClient.invalidateQueries({ queryKey: ["contacts"] });
    }
  }, [
    accessToken,
    activeContact,
    conversationData?.conversationId,
    conversationQueryKey,
    queryClient,
  ]);

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
    void markActiveDuetConversationAsRead(activeContact, conversationData.conversationId);
  }, [conversationData, activeContact, markActiveDuetConversationAsRead]);


  const sendMessageMutation = useSendMessageMutation({
    accessToken,
    activeContactUserId: activeContact?.userId,
    ownerUserId,
    onError: (message) => toast.error(message),
    onSequenceGap: () => void synchronizeMessages(),
  });

  const loadOlderMessages = useCallback(async () => {
    if (!activeContact || !accessToken || isLoadingOlderMessagesRef.current) {
      return;
    }

    const current = queryClient.getQueryData<DuetConversationCacheEntry>(["duetConversation", activeContact.userId]);
    if (!current?.hasMore || current.nextBeforeSequenceNum === null) {
      return;
    }

    isLoadingOlderMessagesRef.current = true;
    setIsLoadingOlderMessages(true);

    try {
      const result = await getDuetConversationMessages(
        current.conversationId,
        current.nextBeforeSequenceNum,
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
            nextBeforeSequenceNum: result.nextBeforeSequenceNum,
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

  const messageReceived = async (
    payload: ChatMessageReceivedEvent,
  ): Promise<number | null> => {
    if (!conversationData || payload.conversationId !== conversationData.conversationId) {
      return null;
    }

    let needsCatchUp = false;
    let requiresRefetch = false;
    queryClient.setQueryData<DuetConversationCacheEntry>(
      conversationQueryKey,
      (current) => {
        if (!current) {
          return current;
        }

        const sender = ownerUserId && payload.senderUserId === ownerUserId ? "me" : "other";
        const result = mergeSequencedMessage(
          current,
          createDuetMessage(
              sender,
              payload.text,
              payload.sentAtUtc,
              payload.messageId,
              payload.conversationId,
              payload.senderUserId,
              payload.sequenceNum,
            ),
        );
        needsCatchUp = result.needsCatchUp;
        requiresRefetch = result.requiresRefetch;
        return result.state;
      },
    );

    if (requiresRefetch) {
      await queryClient.invalidateQueries({ queryKey: conversationQueryKey });
      return null;
    }
    if (needsCatchUp) {
      return synchronizeMessages();
    }
    return queryClient.getQueryData<DuetConversationCacheEntry>(conversationQueryKey)
      ?.lastContiguousSequenceNum ?? null;
  };

  const sendDraft = async (draft: string): Promise<boolean> => {
    const text = draft.trim();
    const conversationId = conversationData?.conversationId;

    if (!text || !conversationId || !accessToken || !ownerUserId || sendMessageMutation.isPending) {
      return false;
    }

    const messageId = crypto.randomUUID();

    try {
      await sendMessageMutation.mutateAsync({ messageId, conversationId, text });
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
    participants: conversationData?.participants ?? [],
    activeConversationId: conversationData?.conversationId ?? null,
    hasConversationError: conversationQueryError !== null,
    isLoadingConversation,
    isSendingMessage: sendMessageMutation.isPending,
    hasOlderMessages: conversationData?.hasMore ?? false,
    isLoadingOlderMessages,
    sendDraft,
    loadOlderMessages,
    messageReceived,
    synchronizeMessages,
    messageSyncStatus: conversationData?.syncStatus ?? "idle",
    lastContiguousSequenceNum: conversationData?.lastContiguousSequenceNum ?? 0,
    markActiveDuetConversationAsRead,
    openContactConversation,
  };
}
