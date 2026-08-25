import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { searchUserProfiles, searchUserProfilesInputSchema } from '../search-user-profiles';

const userId = '00000000-0000-0000-0000-000000000034';

function createUserProfile() {
  return {
    id: userId,
    friendlyUserId: 'alex.morgan',
    firstName: 'Alex',
    lastName: 'Morgan',
    organization: null,
    avatarUrl: null,
  };
}

describe('search user profiles API', () => {
  it('normalizes criteria and validates the response', async () => {
    let requestUrl = '';
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', ({ request }) => {
        requestUrl = request.url;

        return HttpResponse.json({
          items: [createUserProfile()],
          nextCursor: 'alex.morgan',
          hasMore: true,
        });
      }),
    );

    const result = await searchUserProfiles({
      firstName: '  Alex  ',
      lastName: '   ',
    });

    const searchParams = new URL(requestUrl).searchParams;
    expect(searchParams.get('firstName')).toBe('Alex');
    expect(searchParams.has('lastName')).toBe(false);
    expect(searchParams.get('limit')).toBe('20');
    expect(searchParams.has('cursor')).toBe(false);
    expect(result.items[0]).toMatchObject({
      id: userId,
      friendlyUserId: 'alex.morgan',
    });
  });

  it('sends the cursor when loading another page', async () => {
    let requestUrl = '';
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', ({ request }) => {
        requestUrl = request.url;
        return HttpResponse.json({ items: [], nextCursor: null, hasMore: false });
      }),
    );

    await searchUserProfiles({ organization: 'FlowChat' }, 'alex.morgan');

    expect(new URL(requestUrl).searchParams.get('cursor')).toBe('alex.morgan');
  });

  it('requires at least one non-empty search criterion', () => {
    expect(() => searchUserProfilesInputSchema.parse({ firstName: '   ' })).toThrow(
      'Enter at least one search criterion.',
    );
  });

  it('cancels an in-flight request through AbortSignal', async () => {
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', async () => {
        await new Promise((resolve) => window.setTimeout(resolve, 1_000));
        return HttpResponse.json({ items: [], nextCursor: null, hasMore: false });
      }),
    );
    const abortController = new AbortController();

    const request = searchUserProfiles({ organization: 'FlowChat' }, null, abortController.signal);
    abortController.abort();

    await expect(request).rejects.toBeDefined();
  });

  it('rejects an invalid success response', async () => {
    server.use(
      http.get('*/api/userprofiles/search/range/ascending', () =>
        HttpResponse.json({
          items: [{ ...createUserProfile(), id: 'not-a-guid' }],
          nextCursor: null,
          hasMore: false,
        }),
      ),
    );

    await expect(searchUserProfiles({ organization: 'FlowChat' })).rejects.toBeDefined();
  });
});
