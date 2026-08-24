import { renderHook, renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { ChatContextProvider } from '../chat-context-provider';
import { useChatContext } from '../use-chat-context';

function ChatContextProbe() {
  const { activeView, showConversationCreator, showConversations } = useChatContext();

  return (
    <>
      <output>{JSON.stringify(activeView)}</output>
      <button type="button" onClick={() => showConversationCreator('duet')}>
        Show duet creator
      </button>
      <button type="button" onClick={() => showConversationCreator('group')}>
        Show group creator
      </button>
      <button type="button" onClick={showConversations}>
        Show conversations
      </button>
    </>
  );
}

describe('useChatContext', () => {
  it('throws a clear error outside ChatContextProvider', () => {
    expect(() => renderHook(() => useChatContext())).toThrow(
      'useChatContext must be used within ChatContextProvider.',
    );
  });

  it('changes the sidebar while preserving the main window', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <ChatContextProvider>
        <ChatContextProbe />
      </ChatContextProvider>,
    );

    expect(
      screen.getByText(
        JSON.stringify({
          sidebar: { view: 'conversations' },
          mainWindow: { view: 'conversationSelection' },
        }),
      ),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Show duet creator' }));

    expect(
      screen.getByText(
        JSON.stringify({
          sidebar: { view: 'conversationCreator', conversationType: 'duet' },
          mainWindow: { view: 'conversationSelection' },
        }),
      ),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Show conversations' }));

    expect(
      screen.getByText(
        JSON.stringify({
          sidebar: { view: 'conversations' },
          mainWindow: { view: 'conversationSelection' },
        }),
      ),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Show group creator' }));

    expect(
      screen.getByText(
        JSON.stringify({
          sidebar: { view: 'conversationCreator', conversationType: 'group' },
          mainWindow: { view: 'conversationSelection' },
        }),
      ),
    ).toBeInTheDocument();
  });
});
