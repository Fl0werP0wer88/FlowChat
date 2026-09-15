import { delay, http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { DuetConversationList } from '../duet-conversation-list';

const conversations = [
  {
    partnerUserId: '9f3dbb73-989a-48ad-950c-17c945347d97',
    displayName: 'Alex Morgan',
    avatarUrl: 'https://cdn.example.com/alex.jpg',
    email: 'alex@example.com',
    isBlocked: false,
    isBlockedByPartner: false,
    isMuted: false,
    isHidden: false,
    conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
    lastReadMsgSeqNum: 2,
    currentMsgSeqNum: 3,
    unreadCount: 1,
    status: 'Active',
    presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
  },
  {
    partnerUserId: '7adc286d-30ea-4ff2-b2ee-860adcaa98ec',
    displayName: null,
    avatarUrl: null,
    email: 'sam@example.com',
    isBlocked: false,
    isBlockedByPartner: false,
    isMuted: false,
    isHidden: false,
    conversationId: 'b98ce73a-5d8c-450f-bd1a-b756b13de2e6',
    lastReadMsgSeqNum: 0,
    currentMsgSeqNum: 0,
    unreadCount: 0,
    status: 'AFK',
    presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
  },
  {
    partnerUserId: '12a6c871-8d9b-446b-b68b-128f3df8436e',
    displayName: 'Taylor Reed',
    avatarUrl: null,
    email: null,
    isBlocked: false,
    isBlockedByPartner: false,
    isMuted: false,
    isHidden: false,
    conversationId: 'd9e5d04b-af74-4c1c-8c96-1e089ed1cd0d',
    lastReadMsgSeqNum: 4,
    currentMsgSeqNum: 4,
    unreadCount: 0,
    status: 'Busy',
    presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
  },
  {
    partnerUserId: 'b2b4902f-4d9f-4317-a91b-e9ccbbcd90b5',
    displayName: null,
    avatarUrl: null,
    email: null,
    isBlocked: false,
    isBlockedByPartner: false,
    isMuted: false,
    isHidden: false,
    conversationId: '944920e2-f54a-4b7c-908d-2c3c404b6fb7',
    lastReadMsgSeqNum: 1,
    currentMsgSeqNum: 1,
    unreadCount: 0,
    status: 'Invisible',
    presenceChangedAtUtc: '2026-08-18T10:00:00+00:00',
  },
] as const;

describe('DuetConversationList', () => {
  it('renders partner identity and accessible presence indicators', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () => HttpResponse.json({ conversations })),
    );

    const { container } = renderWithProviders(<DuetConversationList />);

    expect(await screen.findByText('Alex Morgan')).toBeInTheDocument();
    expect(screen.getByText('sam@example.com')).toBeInTheDocument();
    expect(screen.getByText('Taylor Reed')).toBeInTheDocument();
    expect(screen.getByText('Unknown user')).toBeInTheDocument();
    expect(container.querySelector('img')).toHaveAttribute(
      'src',
      'https://cdn.example.com/alex.jpg',
    );
    expect(screen.getByText('S')).toBeInTheDocument();
    expect(screen.getByText('TR')).toBeInTheDocument();
    expect(screen.getByText('UU')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Presence: Active' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Presence: AFK' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Presence: Busy' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Presence: Invisible' })).toBeInTheDocument();
  });

  it('renders a skeleton while conversations are loading', () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', async () => {
        await delay('infinite');
        return HttpResponse.json({ conversations: [] });
      }),
    );

    renderWithProviders(<DuetConversationList />);

    expect(screen.getByRole('status', { name: 'Loading conversations' })).toBeInTheDocument();
  });

  it('renders the empty state when there are no duet conversations', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () =>
        HttpResponse.json({ conversations: [] }),
      ),
    );

    renderWithProviders(<DuetConversationList />);

    expect(
      await screen.findByRole('heading', { name: 'No conversations yet' }),
    ).toBeInTheDocument();
  });

  it('renders the response validation message when the payload is invalid', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () =>
        HttpResponse.json({
          conversations: [{ ...conversations[0], status: 'Unknown' }],
        }),
      ),
    );

    renderWithProviders(<DuetConversationList />);

    expect(await screen.findByRole('alert')).toHaveTextContent(/Active.*AFK.*Busy.*Invisible/);
  });

  it('retries after the conversations request fails', async () => {
    let attempt = 0;
    server.use(
      http.get('*/api/aggregate/conversations/duets', () => {
        attempt += 1;
        return attempt === 1
          ? HttpResponse.json({ detail: 'Temporary failure.' }, { status: 500 })
          : HttpResponse.json({ conversations: [conversations[0]] });
      }),
    );
    const user = userEvent.setup();

    renderWithProviders(<DuetConversationList />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Conversations unavailable');
    expect(screen.getByRole('alert')).toHaveTextContent('Temporary failure.');
    await user.click(screen.getByRole('button', { name: 'Try again' }));

    expect(await screen.findByText('Alex Morgan')).toBeInTheDocument();
    expect(attempt).toBe(2);
  });
});
