import { describe, expect, it } from "vitest";
import {
  mapDuetConversationWithPresenceToContact,
  resolveDuetConversationsWithPresence,
} from "./mappers";

describe("duet conversations with presence mapping", () => {
  it("resolves conversations from the gateway response", () => {
    const conversation = { partnerUserId: "partner-id" };

    const result = resolveDuetConversationsWithPresence({ conversations: [conversation] });

    expect(result).toEqual([conversation]);
  });

  it("maps the partner user id to the contact presentation model", () => {
    const result = mapDuetConversationWithPresenceToContact({
      partnerUserId: "partner-id",
      conversationId: "conversation-id",
      currentMsgSeqNum: 8,
      lastReadMsgSeqNum: 3,
      status: "Active",
    });

    expect(result).toMatchObject({
      userId: "partner-id",
      conversationId: "conversation-id",
      unreadCount: 5,
      status: "Active",
    });
  });
});
