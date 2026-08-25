import { http, HttpResponse } from 'msw';

import type { NavigationSidebarState } from '@/features/chat/navigation/context/navigation-context';
import { NavigationContextProvider } from '@/features/chat/navigation/context/navigation-context-provider';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';
import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent, waitFor } from '@/testing/test-utils';

import { ConversationCreator } from '../conversation-creator';

type CreatorType = Extract<
  NavigationSidebarState,
  { view: 'conversationCreator' }
>['conversationType'];

const partnerUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';
const secondPartnerUserId = '24c11faa-8bd7-4608-abcf-26985f3f62be';
const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';

function createUserProfile({
  id = partnerUserId,
  firstName = 'Alex',
  lastName = 'Morgan',
  friendlyUserId = 'alex.morgan',
}: {
  id?: string;
  firstName?: string | null;
  lastName?: string | null;
  friendlyUserId?: string;
} = {}) {
  return {
    id,
    friendlyUserId,
    firstName,
    lastName,
    organization: 'FlowChat',
    avatarUrl: null,
  };
}

function ConversationCreatorHarness({ creatorType }: { creatorType: CreatorType }) {
  const { activeView, showConversationCreator } = useNavigationContext();

  return activeView.sidebar.view === 'conversationCreator' ? (
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
    <NavigationContextProvider>
      <ConversationCreatorHarness creatorType={creatorType} />
    </NavigationContextProvider>,
  );

  await user.click(screen.getByRole('button', { name: 'Open creator' }));
  return user;
}

function useSearchResults(...users: ReturnType<typeof createUserProfile>[]) {
  server.use(
    http.get('*/api/userprofiles/search/range/ascending', () =>
      HttpResponse.json({ items: users, nextCursor: null, hasMore: false }),
    ),
  );
}

describe('ConversationCreator', () => {
  it.each([
    { creatorType: 'duet' as const, heading: 'Create a duet' },
    { creatorType: 'group' as const, heading: 'Create a group' },
  ])('renders the $creatorType creator and returns to conversations', async (scenario) => {
    const user = await renderCreator(scenario.creatorType);

    expect(screen.getByRole('heading', { name: scenario.heading })).toBeInTheDocument();
    expect(screen.getByLabelText('First name')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));

    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });

  it('keeps the group name outside the scrollable user picker', async () => {
    await renderCreator('group');

    const scrollContainer = screen.getByLabelText('First name').closest('.overflow-y-auto');

    expect(scrollContainer).not.toBeNull();
    expect(scrollContainer).not.toContainElement(screen.getByLabelText('Group name'));
  });

  it('creates a duet immediately and blocks the creator while the request is pending', async () => {
    let createPayload: Record<string, unknown> = {};
    let releaseRequest: (() => void) | undefined;
    useSearchResults(createUserProfile());
    server.use(
      http.put('*/api/conversations/duet', async ({ request }) => {
        createPayload = (await request.json()) as Record<string, unknown>;
        await new Promise<void>((resolve) => {
          releaseRequest = resolve;
        });
        return HttpResponse.json({ conversationId, participants: [] }, { status: 201 });
      }),
    );
    const user = await renderCreator('duet');

    await user.type(screen.getByLabelText('First name'), 'Alex');
    await user.click(await screen.findByRole('button', { name: /Alex Morgan/i }));

    await waitFor(() => expect(createPayload).toEqual({ partnerUserId }));
    expect(screen.getByRole('button', { name: 'Back' })).toBeDisabled();
    expect(screen.getByLabelText('First name')).toBeDisabled();

    releaseRequest?.();

    expect(await screen.findByText('Duet conversation created.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open creator' })).toBeInTheDocument();
  });

  it('selects group members, removes one, and submits the remaining participant', async () => {
    let createPayload: Record<string, unknown> = {};
    useSearchResults(
      createUserProfile(),
      createUserProfile({
        id: secondPartnerUserId,
        firstName: 'Sam',
        lastName: 'Lee',
        friendlyUserId: 'sam.lee',
      }),
    );
    server.use(
      http.post('*/api/conversations/group', async ({ request }) => {
        createPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          { conversationId, name: 'Product team', participants: [] },
          { status: 201 },
        );
      }),
    );
    const user = await renderCreator('group');

    await user.type(screen.getByLabelText('Group name'), '  Product team  ');
    await user.type(screen.getByLabelText('Organization'), 'FlowChat');
    await user.click(await screen.findByRole('button', { name: /Alex Morgan/i }));
    await user.click(screen.getByRole('button', { name: /Sam Lee/i }));
    await user.click(screen.getByRole('button', { name: 'Remove Sam Lee' }));
    await user.click(screen.getByRole('button', { name: 'Create group' }));

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

  it('keeps the duet creator open when creation fails', async () => {
    useSearchResults(createUserProfile());
    server.use(
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
