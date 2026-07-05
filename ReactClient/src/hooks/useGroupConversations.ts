import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "../store/authStore";
import type { GroupConversation } from "../types/chat";
import type { GroupConversationChangedEvent, RealtimeChatMessage } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { calculateUnreadCount } from "../utils/chatUtils";
import { useGroupConversationsQuery } from "./queries/useGroupConversationsQuery";

export interface UseGroupConversationsResult {
  groupConversations: GroupConversation[];
  isLoadingGroupConversations: boolean;
  applyGroupConversationChanged: (payload: GroupConversationChangedEvent) => void;
  applyRealtimeMessage: (payload: RealtimeChatMessage, activeGroupConversationId: string | null) => void;
}

function countDistinctParticipants(participantUserIds: string[]): number {
  return new Set(participantUserIds.filter((userId) => userId.trim().length > 0)).size;
}

function mapChangedEventToGroupConversation(payload: GroupConversationChangedEvent): GroupConversation {
  return {
    conversationId: payload.conversationId,
    name: payload.name ?? "",
    participantCount: countDistinctParticipants(payload.participantUserIds ?? []),
    lastReadMsgSeqNum: 0,
    currentMsgSeqNum: 0,
    unreadCount: 0,
  };
}

function withUnreadCount(conversation: GroupConversation): GroupConversation {
  return {
    ...conversation,
    unreadCount: calculateUnreadCount(
      conversation.currentMsgSeqNum,
      conversation.lastReadMsgSeqNum,
    ),
  };
}

export function useGroupConversations(): UseGroupConversationsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();

  const { data: groupConversations = [], isLoading: isLoadingGroupConversations } =
    useGroupConversationsQuery(accessToken, Boolean(accessToken && ownerUserId));

  const applyGroupConversationChanged = (payload: GroupConversationChangedEvent) => {
    const changedConversation = mapChangedEventToGroupConversation(payload);
    if (!changedConversation.conversationId) {
      return;
    }

    queryClient.setQueryData<GroupConversation[]>(["groupConversations"], (current = []) => {
      const existing = current.find(
        (conversation) => conversation.conversationId === changedConversation.conversationId,
      );
      const nextConversation = withUnreadCount({
        ...changedConversation,
        lastReadMsgSeqNum: existing?.lastReadMsgSeqNum ?? changedConversation.lastReadMsgSeqNum,
        currentMsgSeqNum: existing?.currentMsgSeqNum ?? changedConversation.currentMsgSeqNum,
      });

      return [
        nextConversation,
        ...current.filter((conversation) => conversation.conversationId !== changedConversation.conversationId),
      ];
    });
  };

  const applyRealtimeMessage = (
    payload: RealtimeChatMessage,
    activeGroupConversationId: string | null,
  ) => {
    queryClient.setQueryData<GroupConversation[]>(["groupConversations"], (current = []) =>
      current.map((conversation) => {
        if (conversation.conversationId !== payload.conversationId) {
          return conversation;
        }

        const currentMsgSeqNum = Math.max(conversation.currentMsgSeqNum, payload.sequenceNum);
        const lastReadMsgSeqNum = conversation.conversationId === activeGroupConversationId
          ? currentMsgSeqNum
          : conversation.lastReadMsgSeqNum;

        return withUnreadCount({
          ...conversation,
          currentMsgSeqNum,
          lastReadMsgSeqNum,
        });
      }),
    );
  };

  return {
    groupConversations,
    isLoadingGroupConversations,
    applyGroupConversationChanged,
    applyRealtimeMessage,
  };
}
