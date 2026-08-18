import { http, HttpResponse } from 'msw';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { Component as ChatRoute } from '../chat-route';

describe('ChatRoute', () => {
  it('replaces conversations with the creator and restores them on back', async () => {
    server.use(
      http.get('*/api/aggregate/conversations/duets', () =>
        HttpResponse.json({ conversations: [] }),
      ),
    );
    const router = createMemoryRouter([{ path: '/chat', element: <ChatRoute /> }], {
      initialEntries: ['/chat'],
    });
    const user = userEvent.setup();

    renderWithProviders(<RouterProvider router={router} />);

    expect(screen.getByRole('heading', { name: 'Conversations' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Choose a conversation' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'New duet' }));

    expect(screen.getByRole('heading', { name: 'Create a duet' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Conversations' })).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Choose a conversation' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));

    expect(screen.getByRole('heading', { name: 'Conversations' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Create a duet' })).not.toBeInTheDocument();
  });
});
