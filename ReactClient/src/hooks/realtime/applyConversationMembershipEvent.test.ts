import { QueryClient } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";
import type { GroupConversation } from "../../types/chat";
import type {
  ConversationParticipantsAddedEvent,
  ConversationParticipantsRemovedEvent,
} from "../../types/realtime";
import {
  applyContactsMembershipEvent,
  applyGroupConversationsMembershipEvent,
  type ConversationMembershipOperation,
} from "./applyConversationMembershipEvent";

const ownerUserId = "owner-user";
const conversationId = "conversation-id";

function createQueryClient(): QueryClient {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

function createGroupConversation(): GroupConversation {
  return {
    conversationId,
    name: "Dev Team",
    participantCount: 3,
    lastReadMsgSeqNum: 0,
    currentMsgSeqNum: 0,
    unreadCount: 0,
  };
}

describe("conversation membership cache handlers", () => {
  it.each(["added", "removed"])(
    "invalidates contacts when the current user is %s from a duet",
    (operation) => {
      const queryClient = createQueryClient();
      queryClient.setQueryData(["contacts"], [{ userId: ownerUserId }]);
      const payload = operation === "added"
        ? ({
            conversationId,
            conversationType: 1,
            participantUserIds: [ownerUserId],
          } satisfies ConversationParticipantsAddedEvent)
        : ({
            conversationId,
            conversationType: 1,
            participantUserIds: [ownerUserId],
          } satisfies ConversationParticipantsRemovedEvent);

      applyContactsMembershipEvent(queryClient, ownerUserId, payload);

      expect(queryClient.getQueryState(["contacts"])?.isInvalidated).toBe(true);
    },
  );

  it.each<ConversationMembershipOperation>(["added", "removed"])(
    "invalidates the group list and details when the current user is %s",
    (operation) => {
      const queryClient = createQueryClient();
      queryClient.setQueryData(["groupConversations"], [createGroupConversation()]);
      queryClient.setQueryData(["groupConversation", conversationId], { conversationId });

      applyGroupConversationsMembershipEvent(
        queryClient,
        ownerUserId,
        { conversationId, conversationType: 2, participantUserIds: [ownerUserId] },
        operation,
      );

      expect(queryClient.getQueryState(["groupConversations"])?.isInvalidated).toBe(true);
      expect(queryClient.getQueryState(["groupConversation", conversationId])?.isInvalidated).toBe(true);
    },
  );

  it.each([
    ["added", 2, 5],
    ["removed", 2, 1],
  ] as const)(
    "%s participants updates participantCount for an unchanged group member",
    (operation, changedCount, expectedParticipantCount) => {
      const queryClient = createQueryClient();
      queryClient.setQueryData<GroupConversation[]>(["groupConversations"], [createGroupConversation()]);
      queryClient.setQueryData(["groupConversation", conversationId], { conversationId });

      applyGroupConversationsMembershipEvent(
        queryClient,
        ownerUserId,
        {
          conversationId,
          conversationType: 2,
          participantUserIds: Array.from({ length: changedCount }, (_, index) => `changed-${index}`),
        },
        operation,
      );

      expect(queryClient.getQueryData<GroupConversation[]>(["groupConversations"])?.[0].participantCount)
        .toBe(expectedParticipantCount);
      expect(queryClient.getQueryState(["groupConversation", conversationId])?.isInvalidated).toBe(true);
      expect(queryClient.getQueryState(["groupConversations"])?.isInvalidated).toBe(false);
    },
  );
});
