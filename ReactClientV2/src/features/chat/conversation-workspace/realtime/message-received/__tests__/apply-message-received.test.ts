import type { ConversationMessage } from '../../../api/conversation-contracts';
import { createConversationMessageBuffer } from '../../../cache/conversation-message-buffer';
import { applyMessageReceived } from '../apply-message-received';

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

function createState() {
  return createConversationMessageBuffer({
    messages: [createMessage(1), createMessage(2)],
    currentSequenceNum: 2,
  });
}

describe('applyMessageReceived', () => {
  it('returns null when the cache is empty', () => {
    expect(applyMessageReceived(undefined, createMessage(3))).toBeNull();
  });

  it('applies a contiguous message without requesting synchronization', () => {
    const result = applyMessageReceived(createState(), createMessage(3));

    expect(result).toMatchObject({
      state: { lastContiguousSequenceNum: 3 },
      needsCatchUp: false,
      requiresRefetch: false,
    });
  });

  it('buffers a sequence gap and requests catch-up', () => {
    const result = applyMessageReceived(createState(), createMessage(4));

    expect(result).toMatchObject({
      state: { pendingMessagesBySequence: { 4: createMessage(4) } },
      needsCatchUp: true,
      requiresRefetch: false,
    });
  });

  it('requests a refetch for a conflicting sequence number', () => {
    const result = applyMessageReceived(createState(), createMessage(2, 99));

    expect(result).toMatchObject({
      state: { syncStatus: 'error' },
      needsCatchUp: false,
      requiresRefetch: true,
    });
  });
});
