import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "../store/authStore";
import type { GroupConversation } from "../types/chat";
import type {
  ChatMessageReceivedEvent,
  GroupConversationChangedEvent,
  ConversationParticipantsAddedEvent,
  ConversationParticipantsRemovedEvent,
} from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { calculateUnreadCount } from "../utils/chatUtils";
import { useGroupConversationsQuery } from "./queries/useGroupConversationsQuery";
import { applyGroupConversationsMembershipEvent } from "./realtime/applyConversationMembershipEvent";

export interface UseGroupConversationsResult {
  groupConversations: GroupConversation[];
  isLoadingGroupConversations: boolean;
  applyGroupConversationChanged: (payload: GroupConversationChangedEvent) => void;
  applyConversationParticipantsAdded: (payload: ConversationParticipantsAddedEvent) => void;
  applyConversationParticipantsRemoved: (payload: ConversationParticipantsRemovedEvent) => void;
  applyRealtimeMessage: (payload: ChatMessageReceivedEvent, activeGroupConversationId: string | null) => void;
  invalidateGroupConversationsList: () => void;
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

  const { data: groupConversations = [], isLoading: isLoadingGroupConversations } = useGroupConversationsQuery(
    accessToken,
    Boolean(accessToken && ownerUserId),
  );

  const applyGroupConversationChanged = (payload: GroupConversationChangedEvent) => {
    if (!payload.conversationId) {
      return;
    }

    const current = queryClient.getQueryData<GroupConversation[]>(["groupConversations"]) ?? [];
    const existing = current.find((conversation) => conversation.conversationId === payload.conversationId);
    if (!existing) {
      void queryClient.invalidateQueries({ queryKey: ["groupConversations"] });
      return;
    }

    queryClient.setQueryData<GroupConversation[]>(
      ["groupConversations"],
      (current = []) =>
        current.map((conversation) =>
          conversation.conversationId === payload.conversationId
            ? withUnreadCount({ ...conversation, name: payload.name ?? conversation.name })
            : conversation
        ),
    );
  };

  const applyRealtimeMessage = (
    payload: ChatMessageReceivedEvent,
    _activeGroupConversationId: string | null,
  ) => {
    queryClient.setQueryData<GroupConversation[]>(
      ["groupConversations"],
      (current = []) =>
        current.map((conversation) => {
          if (conversation.conversationId !== payload.conversationId) {
            return conversation;
          }

          const currentMsgSeqNum = Math.max(conversation.currentMsgSeqNum, payload.sequenceNum);
          return withUnreadCount({
            ...conversation,
            currentMsgSeqNum,
          });
        }),
    );
  };

  return {
    groupConversations,
    isLoadingGroupConversations,
    applyGroupConversationChanged,
    applyConversationParticipantsAdded: (payload) =>
      applyGroupConversationsMembershipEvent(queryClient, ownerUserId, payload, "added"),
    applyConversationParticipantsRemoved: (payload) =>
      applyGroupConversationsMembershipEvent(queryClient, ownerUserId, payload, "removed"),
    applyRealtimeMessage,
    invalidateGroupConversationsList: () =>
      void queryClient.invalidateQueries({ queryKey: ["groupConversations"] }),
  };
}
