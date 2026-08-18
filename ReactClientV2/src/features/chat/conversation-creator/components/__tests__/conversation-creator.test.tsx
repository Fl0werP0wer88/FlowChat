import type { ChatViewState } from '@/features/chat/context/chat-context';
import { ChatContextProvider } from '@/features/chat/context/chat-context-provider';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { ConversationCreator } from '../conversation-creator';

type CreatorType = Extract<ChatViewState, { view: 'conversationCreator' }>['conversationType'];

function ConversationCreatorHarness({ creatorType }: { creatorType: CreatorType }) {
  const { activeView, showConversationCreator } = useChatContext();

  return activeView.view === 'conversationCreator' ? (
    <ConversationCreator />
  ) : (
    <button type="button" onClick={() => showConversationCreator(creatorType)}>
      Open creator
    </button>
  );
}

describe('ConversationCreator', () => {
  it.each([
    {
      creatorType: 'duet' as const,
      heading: 'Create a duet',
      message: 'The duet conversation form will be available here soon.',
    },
    {
      creatorType: 'group' as const,
      heading: 'Create a group',
      message: 'The group conversation form will be available here soon.',
    },
  ])('renders the $creatorType placeholder and returns to conversations', async (scenario) => {
    const user = userEvent.setup();

    renderWithProviders(
      <ChatContextProvider>
        <ConversationCreatorHarness creatorType={scenario.creatorType} />
      </ChatContextProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Open creator' }));

    expect(screen.getByRole('heading', { name: scenario.heading })).toBeInTheDocument();
    expect(screen.getByText(scenario.message)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));

    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });
});
