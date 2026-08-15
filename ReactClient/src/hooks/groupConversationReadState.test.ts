import { QueryClient } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";
import { resolveGroupConversationReadSequence } from "./groupConversationReadState";

describe("resolveGroupConversationReadSequence", () => {
  it("does not reuse the sequence from the previously active group", () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(
      ["groupConversation", "previous-conversation"],
      { lastContiguousSequenceNum: 42 },
    );

    const sequenceNum = resolveGroupConversationReadSequence(
      queryClient,
      "empty-conversation",
    );

    expect(sequenceNum).toBe(0);
  });

  it("uses the sequence cached for the target group", () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(
      ["groupConversation", "target-conversation"],
      { lastContiguousSequenceNum: 7 },
    );

    const sequenceNum = resolveGroupConversationReadSequence(
      queryClient,
      "target-conversation",
    );

    expect(sequenceNum).toBe(7);
  });

  it("prefers an explicitly supplied sequence", () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(
      ["groupConversation", "target-conversation"],
      { lastContiguousSequenceNum: 7 },
    );

    const sequenceNum = resolveGroupConversationReadSequence(
      queryClient,
      "target-conversation",
      3,
    );

    expect(sequenceNum).toBe(3);
  });
});
