import { http, HttpResponse } from 'msw';

import { ChatContextProvider } from '@/features/chat/context/chat-context-provider';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { GroupConversationsManager } from '../group-conversations-manager';

function CreatorStateProbe() {
  const { activeView } = useChatContext();

  return (
    <output>
      {activeView.sidebar.view === 'conversationCreator'
        ? `${activeView.sidebar.view}:${activeView.sidebar.conversationType}`
        : activeView.sidebar.view}
    </output>
  );
}

describe('GroupConversationsManager', () => {
  it('renders the group action and list, then opens the group creator', async () => {
    server.use(
      http.get('*/api/conversations/group', () =>
        HttpResponse.json({
          groupConversations: [
            {
              conversationId: '40c3cd3b-69d8-4af3-b1a7-f9174537fb97',
              name: 'Product team',
              participantCount: 5,
              lastReadMsgSeqNum: 3,
              currentMsgSeqNum: 7,
            },
          ],
        }),
      ),
    );
    const user = userEvent.setup();

    renderWithProviders(
      <ChatContextProvider>
        <GroupConversationsManager />
        <CreatorStateProbe />
      </ChatContextProvider>,
    );

    expect(await screen.findByText('Product team')).toBeInTheDocument();
    expect(screen.getByText('conversations')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'New group' }));

    expect(screen.getByText('conversationCreator:group')).toBeInTheDocument();
  });
});
