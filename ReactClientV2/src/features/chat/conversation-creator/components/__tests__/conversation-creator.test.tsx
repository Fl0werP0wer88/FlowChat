import { ChatContextProvider } from '@/features/chat/context/chat-context-provider';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { ConversationCreator } from '../conversation-creator';

function ConversationCreatorHarness() {
  const { activeSidebarView, showConversationCreator } = useChatContext();

  return activeSidebarView === 'conversationCreator' ? (
    <ConversationCreator />
  ) : (
    <button type="button" onClick={showConversationCreator}>
      Open creator
    </button>
  );
}

describe('ConversationCreator', () => {
  it('renders its placeholder and returns to conversations', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <ChatContextProvider>
        <ConversationCreatorHarness />
      </ChatContextProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Open creator' }));

    expect(screen.getByRole('heading', { name: 'Create a duet' })).toBeInTheDocument();
    expect(
      screen.getByText('The duet conversation form will be available here soon.'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));

    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });
});
