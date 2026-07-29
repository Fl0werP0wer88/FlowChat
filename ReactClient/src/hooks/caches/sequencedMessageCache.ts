import type { ChatMessage } from "../../types/chat";

export type MessageSyncStatus = "idle" | "syncing" | "error";

export interface SequencedMessageState {
  messages: ChatMessage[];
  lastContiguousSequenceNum: number;
  pendingMessagesBySequence: Record<number, ChatMessage>;
  syncStatus: MessageSyncStatus;
}

export interface MergeResult<TState extends SequencedMessageState> {
  state: TState;
  needsCatchUp: boolean;
  requiresRefetch: boolean;
}

export const PENDING_MESSAGE_LIMIT = 100;

export function sortSequencedMessages(messages: ChatMessage[]): ChatMessage[] {
  return [...messages].sort((left, right) => {
    if (left.sequenceNum !== null && right.sequenceNum !== null) {
      return left.sequenceNum - right.sequenceNum;
    }
    if (left.sequenceNum !== null) return -1;
    if (right.sequenceNum !== null) return 1;
    return left.sentAtUtc.localeCompare(right.sentAtUtc);
  });
}

function upsertVisible(messages: ChatMessage[], incoming: ChatMessage): ChatMessage[] {
  const withoutIncoming = messages.filter((message) => message.id !== incoming.id);
  return sortSequencedMessages([...withoutIncoming, incoming]);
}

export function mergeSequencedMessage<TState extends SequencedMessageState>(
  current: TState,
  incoming: ChatMessage,
): MergeResult<TState> {
  if (incoming.sequenceNum === null) {
    return {
      state: { ...current, messages: upsertVisible(current.messages, incoming) },
      needsCatchUp: false,
      requiresRefetch: false,
    };
  }

  const sequenceNum = incoming.sequenceNum;
  const visibleConflict = current.messages.find(
    (message) => message.sequenceNum === sequenceNum && message.id !== incoming.id,
  );
  const pendingConflict = current.pendingMessagesBySequence[sequenceNum];
  if (visibleConflict || (pendingConflict && pendingConflict.id !== incoming.id)) {
    return {
      state: { ...current, syncStatus: "error" },
      needsCatchUp: false,
      requiresRefetch: true,
    };
  }

  if (sequenceNum <= current.lastContiguousSequenceNum) {
    return {
      state: { ...current, messages: upsertVisible(current.messages, incoming) },
      needsCatchUp: false,
      requiresRefetch: false,
    };
  }

  const pending = { ...current.pendingMessagesBySequence, [sequenceNum]: incoming };
  if (Object.keys(pending).length > PENDING_MESSAGE_LIMIT) {
    return {
      state: { ...current, pendingMessagesBySequence: pending, syncStatus: "error" },
      needsCatchUp: false,
      requiresRefetch: true,
    };
  }

  let watermark = current.lastContiguousSequenceNum;
  let messages = current.messages.filter((message) => message.id !== incoming.id);
  while (pending[watermark + 1]) {
    const next = pending[watermark + 1];
    delete pending[watermark + 1];
    messages = upsertVisible(messages, next);
    watermark += 1;
  }

  return {
    state: {
      ...current,
      messages,
      lastContiguousSequenceNum: watermark,
      pendingMessagesBySequence: pending,
    },
    needsCatchUp: Object.keys(pending).length > 0,
    requiresRefetch: false,
  };
}

export function completeSequenceSnapshot<TState extends SequencedMessageState>(
  current: TState,
  throughSequenceNum: number,
): TState {
  let messages = current.messages;
  const pending = { ...current.pendingMessagesBySequence };

  for (const [key, message] of Object.entries(pending)) {
    if (Number(key) <= throughSequenceNum) {
      messages = upsertVisible(messages, message);
      delete pending[Number(key)];
    }
  }

  let watermark = Math.max(current.lastContiguousSequenceNum, throughSequenceNum);
  while (pending[watermark + 1]) {
    const next = pending[watermark + 1];
    delete pending[watermark + 1];
    messages = upsertVisible(messages, next);
    watermark += 1;
  }

  return {
    ...current,
    messages,
    lastContiguousSequenceNum: watermark,
    pendingMessagesBySequence: pending,
    syncStatus: "idle",
  };
}
