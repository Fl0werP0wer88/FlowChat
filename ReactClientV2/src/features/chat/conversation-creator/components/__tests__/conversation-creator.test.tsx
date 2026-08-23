import { http, HttpResponse } from 'msw';

import type { ChatViewState } from '@/features/chat/context/chat-context';
import { ChatContextProvider } from '@/features/chat/context/chat-context-provider';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { useAuthStore } from '@/stores/auth-store';
import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent, waitFor } from '@/testing/test-utils';

import { ConversationCreator } from '../conversation-creator';

type CreatorType = Extract<ChatViewState, { view: 'conversationCreator' }>['conversationType'];

const currentUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';
const partnerUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';
const secondPartnerUserId = '24c11faa-8bd7-4608-abcf-26985f3f62be';
const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';

function createUserProfile({
  id = partnerUserId,
  firstName = 'Alex',
  lastName = 'Morgan',
  friendlyUserId = 'alex.morgan',
  organization = 'FlowChat',
  avatarUrl = null,
}: {
  id?: string;
  firstName?: string | null;
  lastName?: string | null;
  friendlyUserId?: string;
  organization?: string | null;
  avatarUrl?: string | null;
} = {}) {
  return {
    id,
    friendlyUserId,
    firstName,
    lastName,
    organization,
    avatarUrl,
    bio: null,
    isActive: true,
    lastSeenAtUtc: null,
    emails: [],
    phones: [],
  };
}

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

async function renderCreator(creatorType: CreatorType) {
  const user = userEvent.setup();

  renderWithProviders(
    <ChatContextProvider>
      <ConversationCreatorHarness creatorType={creatorType} />
    </ChatContextProvider>,
  );

  await user.click(screen.getByRole('button', { name: 'Open creator' }));
  return user;
}

function setCurrentUser() {
  useAuthStore.getState().setSession({
    accessToken: 'access-token',
    expiresAtUtc: '2026-08-23T22:00:00+00:00',
    user: {
      id: currentUserId,
      email: 'current@example.com',
      friendlyUserId: 'current.user',
      roles: [],
    },
  });
}

describe('ConversationCreator', () => {
  it.each([
    { creatorType: 'duet' as const, heading: 'Create a duet' },
    { creatorType: 'group' as const, heading: 'Create a group' },
  ])('renders the $creatorType creator and returns to conversations', async (scenario) => {
    const user = await renderCreator(scenario.creatorType);

    expect(screen.getByRole('heading', { name: scenario.heading })).toBeInTheDocument();
    expect(
      screen.getByText('Enter a first name, last name, or organization to find users.'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));

    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });

  it('debounces normalized search, filters the current user, and creates a duet immediately', async () => {
    setCurrentUser();
    let searchUrl = '';
    let createPayload: Record<string, unknown> = {};
    server.use(
      http.get('*/api/userprofiles/search', ({ request }) => {
        searchUrl = request.url;
        return HttpResponse.json({
          userProfiles: [
            createUserProfile({
              id: currentUserId,
              firstName: 'Current',
              lastName: 'User',
              friendlyUserId: 'current.user',
            }),
            createUserProfile({ firstName: null, lastName: null, friendlyUserId: 'quiet.user' }),
          ],
        });
      }),
      http.put('*/api/conversations/duet', async ({ request }) => {
        createPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            conversationId,
            participants: [
              {
                userId: partnerUserId,
                displayName: null,
                avatarUrl: null,
                participantUserId: partnerUserId,
              },
            ],
          },
          { status: 201 },
        );
      }),
    );
    const user = await renderCreator('duet');

    await user.type(screen.getByLabelText('First name'), '  Alex  ');

    const partnerButton = await screen.findByRole('button', { name: /@quiet\.user/i });
    expect(screen.queryByText('Current User')).not.toBeInTheDocument();
    expect(screen.getByText('QU')).toBeInTheDocument();
    expect(new URL(searchUrl).searchParams.get('firstName')).toBe('Alex');

    await user.click(partnerButton);

    await waitFor(() => expect(createPayload).toEqual({ partnerUserId }));
    expect(await screen.findByText('Duet conversation created.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });

  it('selects, toggles, removes, and submits group members', async () => {
    let createPayload: Record<string, unknown> = {};
    server.use(
      http.get('*/api/userprofiles/search', () =>
        HttpResponse.json({
          userProfiles: [
            createUserProfile(),
            createUserProfile({
              id: secondPartnerUserId,
              firstName: 'Sam',
              lastName: 'Lee',
              friendlyUserId: 'sam.lee',
            }),
          ],
        }),
      ),
      http.post('*/api/conversations/group', async ({ request }) => {
        createPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            conversationId,
            name: 'Product team',
            participants: [],
          },
          { status: 201 },
        );
      }),
    );
    const user = await renderCreator('group');
    const createButton = screen.getByRole('button', { name: 'Create group' });

    expect(createButton).toBeDisabled();
    await user.type(screen.getByLabelText('Group name'), '  Product team  ');
    await user.type(screen.getByLabelText('Organization'), 'FlowChat');

    const alexButton = await screen.findByRole('button', { name: /Alex Morgan/i });
    const samButton = screen.getByRole('button', { name: /Sam Lee/i });
    await user.click(alexButton);
    expect(alexButton).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Remove Alex Morgan' })).toBeInTheDocument();

    await user.click(alexButton);
    expect(alexButton).toHaveAttribute('aria-pressed', 'false');
    expect(screen.queryByRole('button', { name: 'Remove Alex Morgan' })).not.toBeInTheDocument();

    await user.click(alexButton);
    await user.click(samButton);
    await user.click(screen.getByRole('button', { name: 'Remove Sam Lee' }));
    expect(createButton).toBeEnabled();

    await user.click(createButton);

    await waitFor(() =>
      expect(createPayload).toEqual({
        conversationId: expect.any(String),
        name: 'Product team',
        participantUserIds: [partnerUserId],
      }),
    );
    expect(await screen.findByText('Group conversation created.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });

  it('shows an empty search result', async () => {
    server.use(
      http.get('*/api/userprofiles/search', () => HttpResponse.json({ userProfiles: [] })),
    );
    const user = await renderCreator('duet');

    await user.type(screen.getByLabelText('Last name'), 'Nobody');

    expect(await screen.findByText('No users match these search criteria.')).toBeInTheDocument();
  });

  it('shows a search error and retries the request', async () => {
    let requestCount = 0;
    server.use(
      http.get('*/api/userprofiles/search', () => {
        requestCount += 1;
        return requestCount === 1
          ? HttpResponse.json({ detail: 'Search service is unavailable.' }, { status: 503 })
          : HttpResponse.json({ userProfiles: [createUserProfile()] });
      }),
    );
    const user = await renderCreator('duet');

    await user.type(screen.getByLabelText('First name'), 'Alex');

    expect(await screen.findByText('Search service is unavailable.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Try again' }));

    expect(await screen.findByRole('button', { name: /Alex Morgan/i })).toBeInTheDocument();
    expect(requestCount).toBe(2);
  });

  it('keeps the duet creator open when creation fails', async () => {
    server.use(
      http.get('*/api/userprofiles/search', () =>
        HttpResponse.json({ userProfiles: [createUserProfile()] }),
      ),
      http.put('*/api/conversations/duet', () =>
        HttpResponse.json({ detail: 'Unable to create this duet.' }, { status: 409 }),
      ),
    );
    const user = await renderCreator('duet');

    await user.type(screen.getByLabelText('First name'), 'Alex');
    await user.click(await screen.findByRole('button', { name: /Alex Morgan/i }));

    expect(await screen.findByText('Unable to create this duet.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Create a duet' })).toBeInTheDocument();
  });
});
