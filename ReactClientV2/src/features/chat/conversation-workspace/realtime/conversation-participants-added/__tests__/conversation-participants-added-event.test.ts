import { parseConversationParticipantsAdded } from '../conversation-participants-added-event';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const participantUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

describe('ConversationParticipantsAdded event', () => {
  it('parses a valid payload', () => {
    expect(
      parseConversationParticipantsAdded({
        conversationId,
        conversationType: 1,
        participantUserIds: [participantUserId],
      }),
    ).toEqual({ conversationId, conversationType: 1, participantUserIds: [participantUserId] });
  });

  it('rejects a malformed payload', () => {
    expect(
      parseConversationParticipantsAdded({
        conversationId,
        conversationType: 3,
        participantUserIds: [participantUserId],
      }),
    ).toBeNull();
  });
});
