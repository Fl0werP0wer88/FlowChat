import { http, HttpResponse } from 'msw';
import { useRef, useState } from 'react';

import { useAuthStore } from '@/stores/auth-store';
import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent, waitFor } from '@/testing/test-utils';

import type { UserProfile } from '../../../api/search-user-profile';
import { UserPicker } from '../user-picker';

const currentUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';
const firstUserId = '14c11faa-8bd7-4608-abcf-26985f3f62be';
const secondUserId = '24c11faa-8bd7-4608-abcf-26985f3f62be';
const intersectionObservers: TestIntersectionObserver[] = [];
const singleSelect = vi.fn();

class TestIntersectionObserver implements IntersectionObserver {
  readonly root: Element | Document | null;
  readonly rootMargin: string;
  readonly thresholds: ReadonlyArray<number>;
  private target: Element | null = null;

  constructor(
    private readonly callback: IntersectionObserverCallback,
    options?: IntersectionObserverInit,
  ) {
    this.root = options?.root ?? null;
    this.rootMargin = options?.rootMargin ?? '0px';
    this.thresholds = Array.isArray(options?.threshold)
      ? options.threshold
      : [options?.threshold ?? 0];
    intersectionObservers.push(this);
  }

  disconnect() {
    this.target = null;
  }

  observe(target: Element) {
    this.target = target;
  }

  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }

  unobserve(target: Element) {
    if (this.target === target) this.target = null;
  }

  trigger() {
    if (!this.target) return;
    this.callback(
      [{ isIntersecting: true, target: this.target } as IntersectionObserverEntry],
      this,
    );
  }
}

function triggerIntersection() {
  intersectionObservers.forEach((observer) => observer.trigger());
}

function createUserProfile({
  id = firstUserId,
  firstName = 'Alex',
  lastName = 'Morgan',
  friendlyUserId = 'alex.morgan',
}: {
  id?: string;
  firstName?: string | null;
  lastName?: string | null;
  friendlyUserId?: string;
} = {}): UserProfile {
  return {
    id,
    friendlyUserId,
    firstName,
    lastName,
    organization: 'FlowChat',
    avatarUrl: null,
  };
}

function UserPickerHarness({ mode = 'single' }: { mode?: 'single' | 'multiple' }) {
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const [selectedUsers, setSelectedUsers] = useState<UserProfile[]>([]);

  const handleSelect = (userProfile: UserProfile) => {
    if (mode === 'single') {
      singleSelect(userProfile);
      return;
    }

    setSelectedUsers((current) =>
      current.some((selectedUser) => selectedUser.id === userProfile.id)
        ? current.filter((selectedUser) => selectedUser.id !== userProfile.id)
        : [...current, userProfile],
    );
  };

  return (
    <div ref={scrollContainerRef}>
      <UserPicker
        mode={mode}
        selectedUsers={selectedUsers}
        disabled={false}
        scrollContainerRef={scrollContainerRef}
        onSelect={handleSelect}
        onRemove={(userProfileId) =>
          setSelectedUsers((current) =>
            current.filter((userProfile) => userProfile.id !== userProfileId),
          )
        }
      />
    </div>
  );
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

describe('UserPicker', () => {
  beforeEach(() => {
    intersectionObservers.length = 0;
    singleSelect.mockClear();
    vi.stubGlobal('IntersectionObserver', TestIntersectionObserver);
  });

  afterEach(() => vi.unstubAllGlobals());

  it('debounces normalized criteria, filters the current user, and selects in single mode', async () => {
    setCurrentUser();
    let searchUrl = '';
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', ({ request }) => {
        searchUrl = request.url;
        return HttpResponse.json({
          items: [
            createUserProfile({
              id: currentUserId,
              firstName: 'Current',
              lastName: 'User',
              friendlyUserId: 'current.user',
            }),
            createUserProfile({ firstName: null, lastName: null, friendlyUserId: 'quiet.user' }),
          ],
          nextCursor: null,
          hasMore: false,
        });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness />);

    expect(
      screen.getByText('Enter a first name, last name, or organization to find users.'),
    ).toBeInTheDocument();
    await user.type(screen.getByLabelText('First name'), '  Alex  ');

    const userButton = await screen.findByRole('button', { name: /@quiet\.user/i });
    expect(screen.queryByText('Current User')).not.toBeInTheDocument();
    expect(new URL(searchUrl).searchParams.get('firstName')).toBe('Alex');
    await user.click(userButton);
    expect(singleSelect).toHaveBeenCalledWith(
      expect.objectContaining({ friendlyUserId: 'quiet.user' }),
    );
  });

  it('selects, toggles, and removes users in multiple mode', async () => {
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', () =>
        HttpResponse.json({
          items: [createUserProfile()],
          nextCursor: null,
          hasMore: false,
        }),
      ),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness mode="multiple" />);
    await user.type(screen.getByLabelText('Organization'), 'FlowChat');

    const userButton = await screen.findByRole('button', { name: /Alex Morgan/i });
    await user.click(userButton);
    expect(userButton).toHaveAttribute('aria-pressed', 'true');
    await user.click(screen.getByRole('button', { name: 'Remove Alex Morgan' }));
    expect(userButton).toHaveAttribute('aria-pressed', 'false');
  });

  it('shows empty and retryable initial error states', async () => {
    let requestCount = 0;
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', () => {
        requestCount += 1;
        return requestCount === 1
          ? HttpResponse.json({ detail: 'Search unavailable.' }, { status: 503 })
          : HttpResponse.json({ items: [], nextCursor: null, hasMore: false });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness />);
    await user.type(screen.getByLabelText('Last name'), 'Nobody');

    expect(await screen.findByText('Search unavailable.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Try again' }));
    expect(await screen.findByText('No users match these search criteria.')).toBeInTheDocument();
  });

  it('appends another page and stops when no next cursor remains', async () => {
    const requestUrls: string[] = [];
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', ({ request }) => {
        requestUrls.push(request.url);
        return new URL(request.url).searchParams.has('cursor')
          ? HttpResponse.json({
              items: [
                createUserProfile({
                  id: secondUserId,
                  firstName: 'Sam',
                  lastName: 'Lee',
                  friendlyUserId: 'sam.lee',
                }),
              ],
              nextCursor: null,
              hasMore: false,
            })
          : HttpResponse.json({
              items: [createUserProfile()],
              nextCursor: 'alex.morgan',
              hasMore: true,
            });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness mode="multiple" />);
    await user.type(screen.getByLabelText('First name'), 'A');
    expect(await screen.findByRole('button', { name: /Alex Morgan/i })).toBeInTheDocument();

    triggerIntersection();

    expect(await screen.findByRole('button', { name: /Sam Lee/i })).toBeInTheDocument();
    expect(new URL(requestUrls[1] ?? '').searchParams.get('cursor')).toBe('alex.morgan');
    triggerIntersection();
    await waitFor(() => expect(requestUrls).toHaveLength(2));
  });

  it('continues after a page containing only the current user', async () => {
    setCurrentUser();
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', ({ request }) =>
        new URL(request.url).searchParams.has('cursor')
          ? HttpResponse.json({
              items: [createUserProfile()],
              nextCursor: null,
              hasMore: false,
            })
          : HttpResponse.json({
              items: [
                createUserProfile({
                  id: currentUserId,
                  firstName: 'Current',
                  lastName: 'User',
                  friendlyUserId: 'current.user',
                }),
              ],
              nextCursor: 'current.user',
              hasMore: true,
            }),
      ),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness />);
    await user.type(screen.getByLabelText('First name'), 'A');
    await waitFor(() => expect(intersectionObservers.some((observer) => observer.root)).toBe(true));
    triggerIntersection();

    expect(await screen.findByRole('button', { name: /Alex Morgan/i })).toBeInTheDocument();
    expect(screen.queryByText('Current User')).not.toBeInTheDocument();
  });

  it('keeps loaded users and retries a failed next page', async () => {
    let nextPageRequestCount = 0;
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', ({ request }) => {
        if (!new URL(request.url).searchParams.has('cursor')) {
          return HttpResponse.json({
            items: [createUserProfile()],
            nextCursor: 'alex.morgan',
            hasMore: true,
          });
        }

        nextPageRequestCount += 1;
        return nextPageRequestCount === 1
          ? HttpResponse.json({ detail: 'Unable to load more users.' }, { status: 503 })
          : HttpResponse.json({
              items: [
                createUserProfile({
                  id: secondUserId,
                  firstName: 'Sam',
                  lastName: 'Lee',
                  friendlyUserId: 'sam.lee',
                }),
              ],
              nextCursor: null,
              hasMore: false,
            });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness mode="multiple" />);
    await user.type(screen.getByLabelText('First name'), 'A');
    expect(await screen.findByRole('button', { name: /Alex Morgan/i })).toBeInTheDocument();
    triggerIntersection();

    expect(await screen.findByText('Unable to load more users.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Alex Morgan/i })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Try again' }));
    expect(await screen.findByRole('button', { name: /Sam Lee/i })).toBeInTheDocument();
  });

  it('resets pages and ignores a stale page after criteria change', async () => {
    let markNextPageStarted: (() => void) | undefined;
    let releaseNextPage: (() => void) | undefined;
    const nextPageStarted = new Promise<void>((resolve) => {
      markNextPageStarted = resolve;
    });
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', async ({ request }) => {
        const params = new URL(request.url).searchParams;
        if (params.has('cursor')) {
          markNextPageStarted?.();
          await new Promise<void>((resolve) => {
            releaseNextPage = resolve;
          });
          return HttpResponse.json({
            items: [
              createUserProfile({
                id: '34c11faa-8bd7-4608-abcf-26985f3f62be',
                firstName: 'Stale',
                lastName: 'Result',
                friendlyUserId: 'stale.result',
              }),
            ],
            nextCursor: null,
            hasMore: false,
          });
        }

        return params.get('firstName') === 'Sam'
          ? HttpResponse.json({
              items: [
                createUserProfile({
                  id: secondUserId,
                  firstName: 'Sam',
                  lastName: 'Lee',
                  friendlyUserId: 'sam.lee',
                }),
              ],
              nextCursor: null,
              hasMore: false,
            })
          : HttpResponse.json({
              items: [createUserProfile()],
              nextCursor: 'alex.morgan',
              hasMore: true,
            });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<UserPickerHarness mode="multiple" />);
    const input = screen.getByLabelText('First name');
    await user.type(input, 'Alex');
    expect(await screen.findByRole('button', { name: /Alex Morgan/i })).toBeInTheDocument();
    triggerIntersection();
    await nextPageStarted;

    await user.clear(input);
    await user.type(input, 'Sam');
    expect(await screen.findByRole('button', { name: /Sam Lee/i })).toBeInTheDocument();
    releaseNextPage?.();
    await new Promise((resolve) => window.setTimeout(resolve, 50));
    expect(screen.queryByRole('button', { name: /Stale Result/i })).not.toBeInTheDocument();
  });
});
