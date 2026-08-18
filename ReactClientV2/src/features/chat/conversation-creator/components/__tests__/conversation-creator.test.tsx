import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { ConversationCreator } from '../conversation-creator';

describe('ConversationCreator', () => {
  it('renders its placeholder and delegates returning to conversations', async () => {
    const user = userEvent.setup();
    const onBack = vi.fn();

    renderWithProviders(<ConversationCreator onBack={onBack} />);

    expect(screen.getByRole('heading', { name: 'Create a duet' })).toBeInTheDocument();
    expect(
      screen.getByText('The duet conversation form will be available here soon.'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledOnce();
  });
});
