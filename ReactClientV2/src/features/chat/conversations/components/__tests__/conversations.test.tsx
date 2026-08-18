import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { Conversations } from '../conversations';

describe('Conversations', () => {
  it('renders duets by default and loads groups only after they are selected', async () => {
    let duetRequestCount = 0;
    let groupRequestCount = 0;
    server.use(
      http.get('*/api/aggregate/conversations/duets', () => {
        duetRequestCount += 1;
        return HttpResponse.json({
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
        });
      }),
      http.get('*/api/conversations/group', () => {
        groupRequestCount += 1;
        return HttpResponse.json({
          groupConversations: [
            {
              conversationId: 'b98ce73a-5d8c-450f-bd1a-b756b13de2e6',
              name: 'Product team',
              participantCount: 5,
              lastReadMsgSeqNum: 3,
              currentMsgSeqNum: 7,
            },
          ],
        });
      }),
    );
    const user = userEvent.setup();
    const onCreateDuet = vi.fn();

    renderWithProviders(<Conversations onCreateDuet={onCreateDuet} />);

    expect(screen.getByRole('region', { name: 'Conversations' })).toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Conversation type' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Duets' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Groups' })).toHaveAttribute('aria-pressed', 'false');
    expect(await screen.findByText('Alex Morgan')).toBeInTheDocument();
    expect(screen.queryByText('Product team')).not.toBeInTheDocument();
    expect(duetRequestCount).toBe(1);
    expect(groupRequestCount).toBe(0);

    await user.click(screen.getByRole('button', { name: 'New duet' }));

    expect(onCreateDuet).toHaveBeenCalledOnce();

    await user.click(screen.getByRole('button', { name: 'Groups' }));

    expect(await screen.findByText('Product team')).toBeInTheDocument();
    expect(screen.queryByText('Alex Morgan')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Groups' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Duets' })).toHaveAttribute('aria-pressed', 'false');
    expect(groupRequestCount).toBe(1);
  });
});
