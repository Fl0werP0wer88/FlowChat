import { describe, expect, it } from "vitest";
import type { ChatMessage } from "../../types/chat";
import {
  completeSequenceSnapshot,
  mergeSequencedMessage,
  PENDING_MESSAGE_LIMIT,
  type SequencedMessageState,
} from "./sequencedMessageCache";

function message(sequenceNum: number | null, id = `message-${sequenceNum}`): ChatMessage {
  return {
    id,
    conversationId: "conversation",
    senderUserId: "sender",
    sender: "other",
    text: id,
    sequenceNum,
    sentAtUtc: `2026-01-01T00:00:${String(sequenceNum ?? 59).padStart(2, "0")}Z`,
  };
}

function state(watermark = 0): SequencedMessageState {
  return {
    messages: [],
    lastContiguousSequenceNum: watermark,
    pendingMessagesBySequence: {},
    syncStatus: "idle",
  };
}

describe("mergeSequencedMessage", () => {
  it("drains reversed contiguous messages", () => {
    let current = mergeSequencedMessage(state(), message(3)).state;
    current = mergeSequencedMessage(current, message(1)).state;
    current = mergeSequencedMessage(current, message(2)).state;

    expect(current.lastContiguousSequenceNum).toBe(3);
    expect(current.messages.map((item) => item.sequenceNum)).toEqual([1, 2, 3]);
    expect(current.pendingMessagesBySequence).toEqual({});
  });

  it("deduplicates redelivery by id", () => {
    const first = mergeSequencedMessage(state(), message(1)).state;
    const duplicate = mergeSequencedMessage(first, { ...message(1), text: "updated" });

    expect(duplicate.requiresRefetch).toBe(false);
    expect(duplicate.state.messages).toHaveLength(1);
    expect(duplicate.state.messages[0].text).toBe("updated");
  });

  it("requires refetch for two ids sharing a sequence", () => {
    const first = mergeSequencedMessage(state(), message(1, "first")).state;
    const conflict = mergeSequencedMessage(first, message(1, "second"));

    expect(conflict.requiresRefetch).toBe(true);
    expect(conflict.state.syncStatus).toBe("error");
  });

  it("keeps messages behind a gap hidden", () => {
    const result = mergeSequencedMessage(state(4), message(6));

    expect(result.needsCatchUp).toBe(true);
    expect(result.state.messages).toEqual([]);
    expect(result.state.lastContiguousSequenceNum).toBe(4);
  });

  it("requires refetch above the pending limit", () => {
    let current = state();
    let lastResult = mergeSequencedMessage(current, message(2));
    current = lastResult.state;
    for (let sequenceNum = 3; sequenceNum <= PENDING_MESSAGE_LIMIT + 2; sequenceNum += 1) {
      lastResult = mergeSequencedMessage(current, message(sequenceNum));
      current = lastResult.state;
    }

    expect(lastResult.requiresRefetch).toBe(true);
  });

  it("replaces an optimistic item with its confirmation", () => {
    const optimistic = mergeSequencedMessage(state(), message(null, "client-id")).state;
    const confirmed = mergeSequencedMessage(
      optimistic,
      { ...message(1, "client-id"), sentAtUtc: "2026-01-01T00:01:00Z" },
    ).state;

    expect(confirmed.messages).toHaveLength(1);
    expect(confirmed.messages[0].sequenceNum).toBe(1);
  });

  it("hides an optimistic item when confirmation reveals a gap", () => {
    const optimistic = mergeSequencedMessage(state(3), message(null, "client-id")).state;
    const confirmed = mergeSequencedMessage(optimistic, message(5, "client-id"));

    expect(confirmed.state.messages).toEqual([]);
    expect(confirmed.state.pendingMessagesBySequence[5]).toBeDefined();
  });
});

describe("completeSequenceSnapshot", () => {
  it("advances through deleted gaps and exposes buffered snapshot items", () => {
    const buffered = mergeSequencedMessage(state(4), message(7)).state;
    const completed = completeSequenceSnapshot(buffered, 8);

    expect(completed.lastContiguousSequenceNum).toBe(8);
    expect(completed.messages.map((item) => item.sequenceNum)).toEqual([7]);
    expect(completed.pendingMessagesBySequence).toEqual({});
  });
});
