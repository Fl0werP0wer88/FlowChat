import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import type { GroupConversation } from "../types/chat";
import type { ChatMessageReceivedEvent } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { getGroupConversationMessages, markConversationAsRead } from "../api/chatService";
import { calculateUnreadCount } from "../utils/chatUtils";
import type { GroupConversationCacheEntry } from "./caches/groupConversationCache";
import {
  createGroupMessage,
  mapGroupConversationMessage,
  sortGroupMessages,
} from "./caches/groupConversationCache";
import { useSendGroupMessageMutation } from "./mutations/useSendGroupMessageMutation";
import { useGroupConversationQuery } from "./queries/useGroupConversationQuery";
import { mergeSequencedMessage } from "./caches/sequencedMessageCache";
import { useConversationMessageSync } from "./useConversationMessageSync";

export function useGroupChatMessages(activeGroupConversation: GroupConversation | null) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();

  const [isLoadingOlderMessages, setIsLoadingOlderMessages] = useState(false);

  const isLoadingOlderMessagesRef = useRef(false);

  const {
    data: conversationData,
    isLoading: isLoadingConversation,
    error: conversationQueryError,
  } = useGroupConversationQuery(activeGroupConversation, accessToken, ownerUserId);
  const conversationQueryKey = useMemo(
    () => ["groupConversation", activeGroupConversation?.conversationId] as const,
    [activeGroupConversation?.conversationId],
  );
  const mapSynchronizedMessage = useCallback(
    (message: Parameters<typeof mapGroupConversationMessage>[0]) =>
      mapGroupConversationMessage(message, ownerUserId),
    [ownerUserId],
  );
  const synchronizeMessages = useConversationMessageSync<GroupConversationCacheEntry>({
    conversationId: conversationData?.conversationId ?? null,
    queryKey: conversationQueryKey,
    accessToken,
    mapMessage: mapSynchronizedMessage,
  });

  const sendMessageMutation = useSendGroupMessageMutation({
    accessToken,
    activeGroupConversationId: activeGroupConversation?.conversationId,
    ownerUserId,
    onError: (message) => toast.error(message),
    onSequenceGap: () => void synchronizeMessages(),
  });

  const loadOlderMessages = useCallback(async () => {
    if (!activeGroupConversation || !accessToken || isLoadingOlderMessagesRef.current) {
      return;
    }

    const current = queryClient.getQueryData<GroupConversationCacheEntry>([
      "groupConversation",
      activeGroupConversation.conversationId,
    ]);
    if (!current?.hasMore || current.nextBeforeSequenceNum === null) {
      return;
    }

    isLoadingOlderMessagesRef.current = true;
    setIsLoadingOlderMessages(true);

    try {
      const result = await getGroupConversationMessages(
        current.conversationId,
        current.nextBeforeSequenceNum,
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
  }, [accessToken, activeGroupConversation, ownerUserId, queryClient]);

  const messageReceived = async (
    payload: ChatMessageReceivedEvent,
  ): Promise<number | null> => {
    if (!conversationData || payload.conversationId !== conversationData.conversationId) {
      return null;
    }

    let needsCatchUp = false;
    let requiresRefetch = false;
    queryClient.setQueryData<GroupConversationCacheEntry>(
      conversationQueryKey,
      (current) => {
        if (!current) {
          return current;
        }

        const sender = ownerUserId && payload.senderUserId === ownerUserId ? "me" : "other";
        const result = mergeSequencedMessage(
          current,
          createGroupMessage(
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
    return queryClient.getQueryData<GroupConversationCacheEntry>(conversationQueryKey)
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

  const markActiveGroupConversationAsRead = useCallback(async (
    conversation?: GroupConversation,
    sequenceNum?: number,
  ) => {
    const targetConversation = conversation ?? activeGroupConversation;

    if (!targetConversation || !accessToken) {
      return;
    }
    const contiguousSequenceNum = sequenceNum
      ?? queryClient.getQueryData<GroupConversationCacheEntry>(conversationQueryKey)
        ?.lastContiguousSequenceNum
      ?? 0;

    queryClient.setQueryData<GroupConversation[]>(["groupConversations"], (current = []) =>
      current.map((item) => {
        if (item.conversationId !== targetConversation.conversationId) {
          return item;
        }

        return {
          ...item,
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
        targetConversation.conversationId,
        contiguousSequenceNum,
        accessToken,
      );
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Nie udalo sie oznaczyc grupy jako przeczytanej.");
      void queryClient.invalidateQueries({ queryKey: ["groupConversations"] });
    }
  }, [accessToken, activeGroupConversation, conversationQueryKey, queryClient]);

  return {
    messages: conversationData?.messages ?? [],
    participants: conversationData?.participants ?? [],
    activeConversationId: conversationData?.conversationId ?? null,
    activeConversationName: conversationData?.name ?? activeGroupConversation?.name ?? null,
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
    markActiveGroupConversationAsRead,
  };
}
