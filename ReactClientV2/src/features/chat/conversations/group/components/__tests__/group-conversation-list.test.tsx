import { delay, http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { GroupConversationList } from '../group-conversation-list';

const groupConversation = {
  conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
  name: 'Product team',
  participantCount: 5,
  lastReadMsgSeqNum: 3,
  currentMsgSeqNum: 7,
};

describe('GroupConversationList', () => {
  it('renders group conversation names', async () => {
    server.use(
      http.get('*/api/conversations/group', () =>
        HttpResponse.json({
          groupConversations: [
            groupConversation,
            {
              ...groupConversation,
              conversationId: 'b98ce73a-5d8c-450f-bd1a-b756b13de2e6',
              name: 'Weekend plans',
            },
          ],
        }),
      ),
    );

    renderWithProviders(<GroupConversationList />);

    expect(await screen.findByText('Product team')).toBeInTheDocument();
    expect(screen.getByText('Weekend plans')).toBeInTheDocument();
  });

  it('renders a skeleton while group conversations are loading', () => {
    server.use(
      http.get('*/api/conversations/group', async () => {
        await delay('infinite');
        return HttpResponse.json({ groupConversations: [] });
      }),
    );

    renderWithProviders(<GroupConversationList />);

    expect(screen.getByRole('status', { name: 'Loading group conversations' })).toBeInTheDocument();
  });

  it('renders the empty state when there are no group conversations', async () => {
    server.use(
      http.get('*/api/conversations/group', () => HttpResponse.json({ groupConversations: [] })),
    );

    renderWithProviders(<GroupConversationList />);

    expect(
      await screen.findByRole('heading', { name: 'No group conversations yet' }),
    ).toBeInTheDocument();
  });

  it('renders the response validation message when the payload is invalid', async () => {
    server.use(
      http.get('*/api/conversations/group', () =>
        HttpResponse.json({
          groupConversations: [{ ...groupConversation, conversationId: 'not-a-guid' }],
        }),
      ),
    );

    renderWithProviders(<GroupConversationList />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid UUID');
  });

  it('retries after the group conversations request fails', async () => {
    let attempt = 0;
    server.use(
      http.get('*/api/conversations/group', () => {
        attempt += 1;
        return attempt === 1
          ? HttpResponse.json({ detail: 'Temporary group failure.' }, { status: 500 })
          : HttpResponse.json({ groupConversations: [groupConversation] });
      }),
    );
    const user = userEvent.setup();

    renderWithProviders(<GroupConversationList />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Temporary group failure.');
    await user.click(screen.getByRole('button', { name: 'Try again' }));

    expect(await screen.findByText('Product team')).toBeInTheDocument();
    expect(attempt).toBe(2);
  });
});
