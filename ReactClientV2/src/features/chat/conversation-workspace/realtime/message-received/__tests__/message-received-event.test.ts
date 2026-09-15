import { parseMessageReceived } from '../message-received-event';

const messageId = 'c7d063d8-0db1-4adf-a425-01f9000b8ace';
const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const senderUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';

function createMessageReceivedPayload() {
  return {
    messageId,
    conversationId,
    senderUserId,
    text: 'Hello',
    sequenceNum: 7,
    sentAtUtc: '2026-08-24T10:00:00+00:00',
    deliveredAtUtc: '2026-08-24T10:00:01+00:00',
  };
}

describe('MessageReceived event', () => {
  it('validates and maps the realtime payload to ConversationMessage', () => {
    expect(parseMessageReceived(createMessageReceivedPayload())).toEqual({
      id: messageId,
      conversationId,
      senderUserId,
      text: 'Hello',
      sequenceNum: 7,
      sentAtUtc: '2026-08-24T10:00:00+00:00',
    });
  });

  it.each([
    { messageId: 'invalid' },
    { sequenceNum: -1 },
    { sentAtUtc: 'invalid' },
    { deliveredAtUtc: 'invalid' },
  ])('ignores a malformed payload: %o', (invalidFields) => {
    expect(
      parseMessageReceived({ ...createMessageReceivedPayload(), ...invalidFields }),
    ).toBeNull();
  });
});
