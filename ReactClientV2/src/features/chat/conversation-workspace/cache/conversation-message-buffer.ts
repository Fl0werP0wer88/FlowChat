import type { ConversationMessage } from '../api/conversation-contracts';

export type MessageSyncStatus = 'idle' | 'syncing' | 'error';

export interface ConversationMessageBufferState {
  messages: ConversationMessage[];
  lastContiguousSequenceNum: number;
  pendingMessagesBySequence: Record<number, ConversationMessage>;
  syncStatus: MessageSyncStatus;
}

export type BufferedConversationSnapshot<
  TSnapshot extends { messages: ConversationMessage[]; currentSequenceNum: number },
> = TSnapshot & ConversationMessageBufferState;

export interface MessageMergeResult<TState extends ConversationMessageBufferState> {
  state: TState;
  needsCatchUp: boolean;
  requiresRefetch: boolean;
}

export const pendingMessageLimit = 100;

export function sortConversationMessages(messages: ConversationMessage[]) {
  return [...messages].sort(
    (left, right) =>
      left.sequenceNum - right.sequenceNum ||
      left.sentAtUtc.localeCompare(right.sentAtUtc) ||
      left.id.localeCompare(right.id),
  );
}

function upsertVisibleMessage(messages: ConversationMessage[], incoming: ConversationMessage) {
  return sortConversationMessages([
    ...messages.filter((message) => message.id !== incoming.id),
    incoming,
  ]);
}

function removePendingMessageId(
  pendingMessages: Record<number, ConversationMessage>,
  messageId: string,
) {
  const pending = { ...pendingMessages };
  for (const [sequenceNum, message] of Object.entries(pending)) {
    if (message.id === messageId) delete pending[Number(sequenceNum)];
  }
  return pending;
}

export function createConversationMessageBuffer<
  TSnapshot extends { messages: ConversationMessage[]; currentSequenceNum: number },
>(snapshot: TSnapshot): BufferedConversationSnapshot<TSnapshot> {
  return {
    ...snapshot,
    messages: sortConversationMessages(snapshot.messages),
    lastContiguousSequenceNum: snapshot.currentSequenceNum,
    pendingMessagesBySequence: {},
    syncStatus: 'idle',
  };
}

export function mergeConversationMessage<TState extends ConversationMessageBufferState>(
  current: TState,
  incoming: ConversationMessage,
): MessageMergeResult<TState> {
  const visibleConflict = current.messages.find(
    (message) => message.sequenceNum === incoming.sequenceNum && message.id !== incoming.id,
  );
  const pendingConflict = current.pendingMessagesBySequence[incoming.sequenceNum];
  if (visibleConflict || (pendingConflict && pendingConflict.id !== incoming.id)) {
    return {
      state: { ...current, syncStatus: 'error' },
      needsCatchUp: false,
      requiresRefetch: true,
    };
  }

  const pending = removePendingMessageId(current.pendingMessagesBySequence, incoming.id);
  if (incoming.sequenceNum <= current.lastContiguousSequenceNum) {
    return {
      state: {
        ...current,
        messages: upsertVisibleMessage(current.messages, incoming),
        pendingMessagesBySequence: pending,
      },
      needsCatchUp: Object.keys(pending).length > 0,
      requiresRefetch: false,
    };
  }

  pending[incoming.sequenceNum] = incoming;
  if (Object.keys(pending).length > pendingMessageLimit) {
    return {
      state: { ...current, pendingMessagesBySequence: pending, syncStatus: 'error' },
      needsCatchUp: false,
      requiresRefetch: true,
    };
  }

  let watermark = current.lastContiguousSequenceNum;
  let messages = current.messages.filter((message) => message.id !== incoming.id);
  while (pending[watermark + 1]) {
    const next = pending[watermark + 1]!;
    delete pending[watermark + 1];
    messages = upsertVisibleMessage(messages, next);
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

export function completeConversationMessageSnapshot<TState extends ConversationMessageBufferState>(
  current: TState,
  throughSequenceNum: number,
): TState {
  let messages = current.messages;
  const pending = { ...current.pendingMessagesBySequence };

  for (const [sequenceNum, message] of Object.entries(pending)) {
    if (Number(sequenceNum) <= throughSequenceNum) {
      messages = upsertVisibleMessage(messages, message);
      delete pending[Number(sequenceNum)];
    }
  }

  let watermark = Math.max(current.lastContiguousSequenceNum, throughSequenceNum);
  while (pending[watermark + 1]) {
    const next = pending[watermark + 1]!;
    delete pending[watermark + 1];
    messages = upsertVisibleMessage(messages, next);
    watermark += 1;
  }

  return {
    ...current,
    messages,
    lastContiguousSequenceNum: watermark,
    pendingMessagesBySequence: pending,
    syncStatus: 'idle',
  };
}
