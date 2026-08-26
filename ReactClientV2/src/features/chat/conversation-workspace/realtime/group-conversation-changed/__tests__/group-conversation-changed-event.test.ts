import { parseGroupConversationChanged } from '../group-conversation-changed-event';

const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';

describe('GroupConversationChanged event', () => {
  it('parses a valid payload', () => {
    expect(
      parseGroupConversationChanged({ conversationId, type: 2, name: 'Product team' }),
    ).toEqual({ conversationId, type: 2, name: 'Product team' });
  });

  it('rejects a malformed payload', () => {
    expect(
      parseGroupConversationChanged({ conversationId, type: 1, name: 'Not a group' }),
    ).toBeNull();
  });
});
