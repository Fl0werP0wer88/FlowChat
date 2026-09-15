import { renderHook, renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { NavigationContextProvider } from '../navigation-context-provider';
import { useNavigationContext } from '../use-navigation-context';

function NavigationContextProbe() {
  const { activeView, showConversationCreator, showConversations } = useNavigationContext();

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

describe('useNavigationContext', () => {
  it('throws a clear error outside NavigationContextProvider', () => {
    expect(() => renderHook(() => useNavigationContext())).toThrow(
      'useNavigationContext must be used within NavigationContextProvider.',
    );
  });

  it('changes the sidebar while preserving the main window', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <NavigationContextProvider>
        <NavigationContextProbe />
      </NavigationContextProvider>,
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
