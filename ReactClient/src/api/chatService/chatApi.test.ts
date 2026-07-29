import { beforeEach, describe, expect, it, vi } from "vitest";
import { getJson } from "../httpClient";
import { catchUpConversationMessages } from "./chatApi";

vi.mock("../httpClient", () => ({
  deleteJson: vi.fn(),
  getJson: vi.fn(),
  postJson: vi.fn(),
  putJson: vi.fn(),
}));

describe("chatApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("uses the dedicated catch-up route and maps its response", async () => {
    vi.mocked(getJson).mockResolvedValue({
      items: [
        {
          id: "message-id",
          conversationId: "conversation-id",
          senderUserId: "sender-id",
          text: "hello",
          sentAtUtc: "2026-07-29T10:00:00+00:00",
          sequenceNum: 11,
        },
      ],
      nextAfterSequenceNum: 11,
      currentSequenceNum: 20,
      throughSequenceNum: 15,
      hasMore: true,
    });

    const result = await catchUpConversationMessages(
      "conversation-id",
      10,
      15,
      "access-token",
    );

    expect(getJson).toHaveBeenCalledWith(
      "/api/chat/conversations/conversation-id/messages/catch-up?afterSequenceNum=10&limit=100&throughSequenceNum=15",
      { accessToken: "access-token", signal: undefined },
    );
    expect(result.nextAfterSequenceNum).toBe(11);
    expect(result.currentSequenceNum).toBe(20);
    expect(result.throughSequenceNum).toBe(15);
    expect(result.messages).toHaveLength(1);
  });
});
