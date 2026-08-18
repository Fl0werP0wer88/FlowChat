import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen } from '@/testing/test-utils';

import { Conversations } from '../conversations';

describe('Conversations', () => {
  it('renders the duet conversation list inside the labelled sidebar section', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () =>
        HttpResponse.json({
          conversations: [
            {
              partnerUserId: '9f3dbb73-989a-48ad-950c-17c945347d97',
              displayName: 'Alex Morgan',
              avatarUrl: null,
              email: 'alex@example.com',
              isBlocked: false,
              isBlockedByPartner: false,
              isMuted: false,
              isHidden: false,
              conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
              lastReadMsgSeqNum: 0,
              currentMsgSeqNum: 0,
              unreadCount: 0,
              status: 'Active',
              presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
            },
          ],
        }),
      ),
    );

    renderWithProviders(<Conversations />);

    expect(screen.getByRole('region', { name: 'Conversations' })).toBeInTheDocument();
    expect(await screen.findByText('Alex Morgan')).toBeInTheDocument();
  });
});
