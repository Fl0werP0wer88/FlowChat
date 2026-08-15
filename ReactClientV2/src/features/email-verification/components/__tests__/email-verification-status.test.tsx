import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';

import { server } from '@/testing/mocks/server';
import { renderWithProviders, screen, userEvent, waitFor } from '@/testing/test-utils';

import { EmailVerificationStatus } from '../email-verification-status';

describe('EmailVerificationStatus', () => {
  it('confirms a valid token once and renders success', async () => {
    let requestCount = 0;
    server.use(
      http.post('*/api/userprofiles/email-verification/confirm', () => {
        requestCount += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderWithProviders(
      <MemoryRouter>
        <EmailVerificationStatus token="valid-token" />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: 'Email confirmed' })).toBeInTheDocument();
    expect(requestCount).toBe(1);
  });

  it('does not call the API when the token is missing', () => {
    renderWithProviders(
      <MemoryRouter>
        <EmailVerificationStatus token={null} />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'Verification token missing' })).toBeInTheDocument();
  });

  it('allows a failed confirmation to be retried', async () => {
    let attempt = 0;
    server.use(
      http.post('*/api/userprofiles/email-verification/confirm', () => {
        attempt += 1;
        return attempt === 1
          ? HttpResponse.json({ detail: 'Temporary verification failure.' }, { status: 500 })
          : new HttpResponse(null, { status: 204 });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(
      <MemoryRouter>
        <EmailVerificationStatus token="retry-token" />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Temporary verification failure.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Try again' }));

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Email confirmed' })).toBeInTheDocument(),
    );
    expect(attempt).toBe(2);
  });
});
