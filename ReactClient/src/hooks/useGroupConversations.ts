import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "../store/authStore";
import type { GroupConversation } from "../types/chat";
import type {
  ChatMessageReceivedEvent,
  GroupConversationChangedEvent,
  GroupConversationParticipantsAddedEvent,
  GroupConversationParticipantsRemovedEvent,
} from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import { calculateUnreadCount } from "../utils/chatUtils";
import { useGroupConversationsQuery } from "./queries/useGroupConversationsQuery";

export interface UseGroupConversationsResult {
  groupConversations: GroupConversation[];
  isLoadingGroupConversations: boolean;
  applyGroupConversationChanged: (payload: GroupConversationChangedEvent) => void;
  applyGroupConversationParticipantsAdded: (payload: GroupConversationParticipantsAddedEvent) => void;
  applyGroupConversationParticipantsRemoved: (payload: GroupConversationParticipantsRemovedEvent) => void;
  applyRealtimeMessage: (payload: ChatMessageReceivedEvent, activeGroupConversationId: string | null) => void;
}

function dedupeParticipantIds(participantUserIds: string[]): string[] {
  return [...new Set(participantUserIds.filter((userId) => userId.trim().length > 0))];
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

  const applyGroupConversationParticipantsAdded = (payload: GroupConversationParticipantsAddedEvent) => {
    const addedUserIds = dedupeParticipantIds(payload.participantUserIds ?? []);
    if (!payload.conversationId || addedUserIds.length === 0) {
      return;
    }
    // ToDo: Inwalidujemy cala konwersacje tylko po to zeby zaktualizowac participantCount to chyba przesada.
    void queryClient.invalidateQueries({ queryKey: ["groupConversation", payload.conversationId] });

    if (ownerUserId && addedUserIds.includes(ownerUserId)) {
      void queryClient.invalidateQueries({ queryKey: ["groupConversations"] });
      return;
    }

    const current = queryClient.getQueryData<GroupConversation[]>(["groupConversations"]) ?? [];
    const existing = current.find((conversation) => conversation.conversationId === payload.conversationId);
    if (!existing) {
      return;
    }

    queryClient.setQueryData<GroupConversation[]>(
      ["groupConversations"],
      (current = []) =>
        current.map((conversation) =>
          conversation.conversationId === payload.conversationId
            ? { ...conversation, participantCount: conversation.participantCount + addedUserIds.length }
            : conversation
        ),
    );
  };

  const applyGroupConversationParticipantsRemoved = (payload: GroupConversationParticipantsRemovedEvent) => {
    const removedUserIds = dedupeParticipantIds(payload.participantUserIds ?? []);
    if (!payload.conversationId || removedUserIds.length === 0) {
      return;
    }
    // ToDo: Inwalidujemy cala konwersacje tylko po to zeby zaktualizowac participantCount to chyba przesada.
    void queryClient.invalidateQueries({ queryKey: ["groupConversation", payload.conversationId] });

    if (ownerUserId && removedUserIds.includes(ownerUserId)) {
      queryClient.setQueryData<GroupConversation[]>(
        ["groupConversations"],
        (current = []) => current.filter((conversation) => conversation.conversationId !== payload.conversationId),
      );
      return;
    }

    queryClient.setQueryData<GroupConversation[]>(
      ["groupConversations"],
      (current = []) =>
        current.map((conversation) =>
          conversation.conversationId === payload.conversationId
            ? { ...conversation, participantCount: Math.max(0, conversation.participantCount - removedUserIds.length) }
            : conversation
        ),
    );
  };

  const applyRealtimeMessage = (
    payload: ChatMessageReceivedEvent,
    activeGroupConversationId: string | null,
  ) => {
    queryClient.setQueryData<GroupConversation[]>(
      ["groupConversations"],
      (current = []) =>
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
    applyGroupConversationParticipantsAdded,
    applyGroupConversationParticipantsRemoved,
    applyRealtimeMessage,
  };
}
