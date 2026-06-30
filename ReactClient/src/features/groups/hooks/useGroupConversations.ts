import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "../../../store/authStore";
import type { GroupConversationChangedEvent } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import type { GroupConversation } from "../../../api/chatApi";
import { useGroupConversationsQuery } from "../queries/useGroupConversationsQuery";

export interface UseGroupConversationsResult {
  groupConversations: GroupConversation[];
  isLoadingGroupConversations: boolean;
  applyGroupConversationChanged: (payload: GroupConversationChangedEvent) => void;
}

function countDistinctParticipants(participantUserIds: string[]): number {
  return new Set(participantUserIds.filter((userId) => userId.trim().length > 0)).size;
}

function mapChangedEventToGroupConversation(payload: GroupConversationChangedEvent): GroupConversation {
  return {
    conversationId: payload.conversationId,
    name: payload.name ?? "",
    participantCount: countDistinctParticipants(payload.participantUserIds ?? []),
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

    queryClient.setQueryData<GroupConversation[]>(["groupConversations"], (current = []) => [
      changedConversation,
      ...current.filter((conversation) => conversation.conversationId !== changedConversation.conversationId),
    ]);
  };

  return { groupConversations, isLoadingGroupConversations, applyGroupConversationChanged };
}
