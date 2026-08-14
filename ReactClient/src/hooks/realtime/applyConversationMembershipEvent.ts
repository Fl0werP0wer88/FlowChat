import type { QueryClient } from "@tanstack/react-query";
import type { GroupConversation } from "../../types/chat";
import type {
  ConversationParticipantsAddedEvent,
  ConversationParticipantsRemovedEvent,
} from "../../types/realtime";

type ConversationMembershipEvent =
  | ConversationParticipantsAddedEvent
  | ConversationParticipantsRemovedEvent;

export type ConversationMembershipOperation = "added" | "removed";

const duetConversationType = 1;
const groupConversationType = 2;

function dedupeParticipantIds(participantUserIds: string[]): string[] {
  return [...new Set(participantUserIds.filter((userId) => userId.trim().length > 0))];
}

export function applyContactsMembershipEvent(
  queryClient: QueryClient,
  ownerUserId: string | null,
  payload: ConversationMembershipEvent,
): void {
  if (
    payload.conversationType !== duetConversationType ||
    !ownerUserId ||
    !dedupeParticipantIds(payload.participantUserIds ?? []).includes(ownerUserId)
  ) {
    return;
  }

  void queryClient.invalidateQueries({ queryKey: ["contacts"] });
}

export function applyGroupConversationsMembershipEvent(
  queryClient: QueryClient,
  ownerUserId: string | null,
  payload: ConversationMembershipEvent,
  operation: ConversationMembershipOperation,
): void {
  const changedUserIds = dedupeParticipantIds(payload.participantUserIds ?? []);
  if (
    payload.conversationType !== groupConversationType ||
    !payload.conversationId ||
    changedUserIds.length === 0
  ) {
    return;
  }

  void queryClient.invalidateQueries({ queryKey: ["groupConversation", payload.conversationId] });

  if (ownerUserId && changedUserIds.includes(ownerUserId)) {
    void queryClient.invalidateQueries({ queryKey: ["groupConversations"] });
    return;
  }

  const participantCountDelta = operation === "added" ? changedUserIds.length : -changedUserIds.length;
  queryClient.setQueryData<GroupConversation[]>(
    ["groupConversations"],
    (current = []) => current.map((conversation) =>
      conversation.conversationId === payload.conversationId
        ? {
            ...conversation,
            participantCount: Math.max(0, conversation.participantCount + participantCountDelta),
          }
        : conversation),
  );
}
