import type { GetDuetsWithPresenceResponse } from '../../api/get-duets-with-presence';
import { applyPresenceChanged } from '../presence-changed';

const partnerUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';

function createResponse(): GetDuetsWithPresenceResponse {
  return {
    conversations: [
      {
        partnerUserId,
        displayName: 'Alex Morgan',
        avatarUrl: null,
        email: 'alex@example.com',
        isBlocked: false,
        isBlockedByPartner: false,
        isMuted: false,
        isHidden: false,
        conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
        lastReadMsgSeqNum: 4,
        currentMsgSeqNum: 7,
        unreadCount: 3,
        status: 'Active',
        presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
      },
    ],
  };
}

describe('applyPresenceChanged', () => {
  it('updates the matching duet presence', () => {
    const current = createResponse();
    const result = applyPresenceChanged(current, {
      userId: partnerUserId,
      status: 'Busy',
      changedAtUtc: '2026-08-18T10:01:00+00:00',
    });

    expect(result?.conversations[0]).toMatchObject({
      status: 'Busy',
      presenceChangedAtUtc: '2026-08-18T10:01:00+00:00',
    });
  });

  it('preserves the cache when the partner is unknown', () => {
    const current = createResponse();
    const result = applyPresenceChanged(current, {
      userId: '82f59930-6de3-4e1d-99f8-8ff1654b9308',
      status: 'Busy',
      changedAtUtc: '2026-08-18T10:01:00+00:00',
    });

    expect(result).toBe(current);
  });

  it('preserves an empty cache', () => {
    expect(
      applyPresenceChanged(undefined, {
        userId: partnerUserId,
        status: 'Busy',
        changedAtUtc: '2026-08-18T10:01:00+00:00',
      }),
    ).toBeUndefined();
  });

  it('rejects an invalid notification', () => {
    const current = createResponse();

    expect(applyPresenceChanged(current, { userId: partnerUserId, status: 'Unknown' })).toBe(
      current,
    );
  });

  it('does not overwrite newer presence with an older notification', () => {
    const current = createResponse();
    const result = applyPresenceChanged(current, {
      userId: partnerUserId,
      status: 'AFK',
      changedAtUtc: '2026-08-18T09:59:00+00:00',
    });

    expect(result).toBe(current);
    expect(result?.conversations[0]?.status).toBe('Active');
  });
});
