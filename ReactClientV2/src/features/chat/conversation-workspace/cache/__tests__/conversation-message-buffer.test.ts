import type { ConversationMessage } from '../../api/conversation-contracts';
import {
  completeConversationMessageSnapshot,
  createConversationMessageBuffer,
  mergeConversationMessage,
  pendingMessageLimit,
  type ConversationMessageBufferState,
} from '../conversation-message-buffer';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const senderUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';

function createMessage(sequenceNum: number, idSuffix = sequenceNum): ConversationMessage {
  return {
    id: `00000000-0000-4000-8000-${idSuffix.toString().padStart(12, '0')}`,
    conversationId,
    senderUserId,
    text: `Message ${sequenceNum}`,
    sentAtUtc: `2026-08-24T10:00:${sequenceNum.toString().padStart(2, '0')}+00:00`,
    sequenceNum,
  };
}

function createState(currentSequenceNum = 2): ConversationMessageBufferState {
  return createConversationMessageBuffer({
    messages: [createMessage(2), createMessage(1)],
    currentSequenceNum,
  });
}

describe('conversation message buffer', () => {
  it('sorts the initial messages and initializes the sequence state', () => {
    const state = createState();

    expect(state.messages.map((message) => message.sequenceNum)).toEqual([1, 2]);
    expect(state.lastContiguousSequenceNum).toBe(2);
    expect(state.pendingMessagesBySequence).toEqual({});
    expect(state.syncStatus).toBe('idle');
  });

  it('buffers an out-of-order message and drains the entire contiguous range', () => {
    const buffered = mergeConversationMessage(createState(), createMessage(4));

    expect(buffered.needsCatchUp).toBe(true);
    expect(buffered.state.lastContiguousSequenceNum).toBe(2);
    expect(buffered.state.pendingMessagesBySequence).toHaveProperty('4');

    const completed = mergeConversationMessage(buffered.state, createMessage(3));

    expect(completed.needsCatchUp).toBe(false);
    expect(completed.state.lastContiguousSequenceNum).toBe(4);
    expect(completed.state.pendingMessagesBySequence).toEqual({});
    expect(completed.state.messages.map((message) => message.sequenceNum)).toEqual([1, 2, 3, 4]);
  });

  it('upserts a repeated message id without adding a duplicate', () => {
    const incoming = { ...createMessage(2), text: 'Updated message' };
    const result = mergeConversationMessage(createState(), incoming);

    expect(result.requiresRefetch).toBe(false);
    expect(result.state.messages).toHaveLength(2);
    expect(result.state.messages[1]).toMatchObject(incoming);
  });

  it('requires a refetch when one sequence number has different message ids', () => {
    const result = mergeConversationMessage(createState(), createMessage(2, 99));

    expect(result.requiresRefetch).toBe(true);
    expect(result.needsCatchUp).toBe(false);
    expect(result.state.syncStatus).toBe('error');
  });

  it('requires a refetch after the pending buffer limit is exceeded', () => {
    const pendingMessagesBySequence = Object.fromEntries(
      Array.from({ length: pendingMessageLimit }, (_, index) => {
        const sequenceNum = index + 4;
        return [sequenceNum, createMessage(sequenceNum)];
      }),
    );
    const state = { ...createState(), pendingMessagesBySequence };

    const result = mergeConversationMessage(state, createMessage(pendingMessageLimit + 4));

    expect(result.requiresRefetch).toBe(true);
    expect(result.state.syncStatus).toBe('error');
    expect(Object.keys(result.state.pendingMessagesBySequence)).toHaveLength(
      pendingMessageLimit + 1,
    );
  });

  it('finalizes a catch-up snapshot and then drains realtime messages after it', () => {
    const state: ConversationMessageBufferState = {
      ...createState(),
      pendingMessagesBySequence: {
        3: createMessage(3),
        4: createMessage(4),
      },
      syncStatus: 'syncing',
    };

    const result = completeConversationMessageSnapshot(state, 3);

    expect(result.lastContiguousSequenceNum).toBe(4);
    expect(result.pendingMessagesBySequence).toEqual({});
    expect(result.messages.map((message) => message.sequenceNum)).toEqual([1, 2, 3, 4]);
    expect(result.syncStatus).toBe('idle');
  });
});
