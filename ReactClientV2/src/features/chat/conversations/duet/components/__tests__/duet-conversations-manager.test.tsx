import { http, HttpResponse } from 'msw';

import { ChatContextProvider } from '@/features/chat/context/chat-context-provider';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { DuetConversationsManager } from '../duet-conversations-manager';

function SidebarViewProbe() {
  const { activeView } = useChatContext();

  return (
    <output>
      {activeView.view === 'conversationCreator'
        ? `${activeView.view}:${activeView.conversationType}`
        : activeView.view}
    </output>
  );
}

describe('DuetConversationsManager', () => {
  it('renders the duet action and list, then opens the creator', async () => {
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
    const user = userEvent.setup();

    renderWithProviders(
      <ChatContextProvider>
        <DuetConversationsManager />
        <SidebarViewProbe />
      </ChatContextProvider>,
    );

    expect(await screen.findByText('Alex Morgan')).toBeInTheDocument();
    expect(screen.getByText('conversations')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'New duet' }));

    expect(screen.getByText('conversationCreator:duet')).toBeInTheDocument();
  });
});
