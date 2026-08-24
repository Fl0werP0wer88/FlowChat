import {
  parseConversationParticipantsAdded,
  parseConversationParticipantsRemoved,
} from '../conversation-participants-changed';
import { parseGroupConversationChanged } from '../group-conversation-changed';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const participantUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

describe('conversation realtime events', () => {
  it('parses a group conversation change', () => {
    expect(
      parseGroupConversationChanged({ conversationId, type: 2, name: 'Product team' }),
    ).toEqual({ conversationId, type: 2, name: 'Product team' });
  });

  it('parses added and removed participants for supported conversation types', () => {
    expect(
      parseConversationParticipantsAdded({
        conversationId,
        conversationType: 1,
        participantUserIds: [participantUserId],
      }),
    ).toEqual({ conversationId, conversationType: 1, participantUserIds: [participantUserId] });
    expect(
      parseConversationParticipantsRemoved({
        conversationId,
        conversationType: 2,
        participantUserIds: [participantUserId],
      }),
    ).toEqual({ conversationId, conversationType: 2, participantUserIds: [participantUserId] });
  });

  it('ignores malformed conversation events', () => {
    expect(
      parseGroupConversationChanged({ conversationId, type: 1, name: 'Not a group' }),
    ).toBeNull();
    expect(
      parseConversationParticipantsAdded({
        conversationId,
        conversationType: 3,
        participantUserIds: [participantUserId],
      }),
    ).toBeNull();
    expect(
      parseConversationParticipantsRemoved({
        conversationId,
        conversationType: 2,
        participantUserIds: ['invalid'],
      }),
    ).toBeNull();
  });
});
