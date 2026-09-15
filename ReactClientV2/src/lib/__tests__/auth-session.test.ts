import { http, HttpResponse } from 'msw';

import { api } from '@/lib/api-client';
import { bootstrapSession, refreshSession } from '@/lib/auth-session';
import { useAuthStore } from '@/stores/auth-store';
import { server } from '@/testing/mocks/server';
import { createAccessToken } from '@/testing/token-fixture';

describe('auth session', () => {
  it('deduplicates concurrent refresh requests', async () => {
    let requestCount = 0;
    server.use(
      http.post('*/api/users/refresh-token', async () => {
        requestCount += 1;
        await new Promise((resolve) => setTimeout(resolve, 10));
        return HttpResponse.json({ access_token: createAccessToken(), expires_in: 3_600 });
      }),
    );

    const [first, second] = await Promise.all([refreshSession(), refreshSession()]);

    expect(requestCount).toBe(1);
    expect(first?.accessToken).toBe(second?.accessToken);
    expect(useAuthStore.getState().session?.user.friendlyUserId).toBe('alex.morgan');
  });

  it('finishes bootstrap anonymously when the refresh cookie is unavailable', async () => {
    useAuthStore.setState({ session: null, bootstrapStatus: 'idle' });

    await bootstrapSession();

    expect(useAuthStore.getState().bootstrapStatus).toBe('ready');
    expect(useAuthStore.getState().session).toBeNull();
  });

  it('refreshes and retries an authenticated request once after a 401', async () => {
    let protectedRequestCount = 0;
    let refreshRequestCount = 0;
    server.use(
      http.get('*/api/protected', () => {
        protectedRequestCount += 1;
        return protectedRequestCount === 1
          ? new HttpResponse(null, { status: 401 })
          : HttpResponse.json({ value: 'ok' });
      }),
      http.post('*/api/users/refresh-token', () => {
        refreshRequestCount += 1;
        return HttpResponse.json({ access_token: createAccessToken(), expires_in: 3_600 });
      }),
    );

    const response = await api.get<{ value: string }>('/api/protected');

    expect(response.data.value).toBe('ok');
    expect(protectedRequestCount).toBe(2);
    expect(refreshRequestCount).toBe(1);
  });
});
