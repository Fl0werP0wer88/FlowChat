import { http, HttpResponse } from 'msw';

import { server } from '@/testing/mocks/server';

import { searchUserProfiles, searchUserProfilesInputSchema } from '../search-user-profile';

const userId = '00000000-0000-0000-0000-000000000034';
const emailId = '30c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const phoneId = '50c3cd3b-69d8-4af3-b1a7-f9174537fb97';

describe('search user profiles API', () => {
  it('normalizes criteria and validates the response', async () => {
    let requestUrl = '';
    server.use(
      http.get('*/api/userprofiles/search', ({ request }) => {
        requestUrl = request.url;

        return HttpResponse.json({
          userProfiles: [
            {
              id: userId,
              friendlyUserId: 'alex.morgan',
              firstName: 'Alex',
              lastName: 'Morgan',
              organization: null,
              avatarUrl: null,
              bio: null,
              isActive: true,
              lastSeenAtUtc: '2026-08-23T18:30:00+00:00',
              emails: [
                {
                  id: emailId,
                  address: 'alex@example.com',
                  isMain: true,
                  isAuth: true,
                  isConfirmed: true,
                  isVisible: true,
                },
              ],
              phones: [
                {
                  id: phoneId,
                  number: '+48123456789',
                  isMain: true,
                  isConfirmed: true,
                  isVisible: false,
                },
              ],
            },
          ],
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
    expect(result.userProfiles[0]).toMatchObject({
      id: userId,
      friendlyUserId: 'alex.morgan',
    });
  });

  it('requires at least one non-empty search criterion', () => {
    expect(() => searchUserProfilesInputSchema.parse({ firstName: '   ' })).toThrow(
      'Enter at least one search criterion.',
    );
  });

  it('rejects an invalid success response', async () => {
    server.use(
      http.get('*/api/userprofiles/search', () =>
        HttpResponse.json({ userProfiles: [{ id: 'not-a-guid' }] }),
      ),
    );

    await expect(searchUserProfiles({ organization: 'FlowChat' })).rejects.toBeDefined();
  });
});
