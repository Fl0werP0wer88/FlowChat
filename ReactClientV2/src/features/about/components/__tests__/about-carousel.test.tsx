import { renderWithProviders, screen, userEvent } from '@/testing/test-utils';

import { AboutCarousel } from '../about-carousel';

describe('AboutCarousel', () => {
  it('moves between sections with the visible controls', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AboutCarousel />);

    expect(
      screen.getByRole('heading', { name: 'I turn complex ideas into clear digital experiences.' }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Show next section' }));

    expect(
      screen.getByRole('heading', {
        name: 'FlowChat makes room for conversations that matter.',
      }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Show previous section' }));

    expect(screen.getByText('About Me')).toBeInTheDocument();
  });

  it('supports left and right arrow keys', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AboutCarousel />);
    const nextButton = screen.getByRole('button', { name: 'Show next section' });

    nextButton.focus();
    await user.keyboard('{ArrowRight}');
    expect(screen.getByText('About the Project')).toBeInTheDocument();

    await user.keyboard('{ArrowLeft}');
    expect(screen.getByText('About Me')).toBeInTheDocument();
  });

  it('uses diamond indicators for direct section selection', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AboutCarousel />);
    const aboutMeIndicator = screen.getByRole('button', { name: 'Show About Me section' });
    const projectIndicator = screen.getByRole('button', {
      name: 'Show About the Project section',
    });

    expect(aboutMeIndicator).toHaveAttribute('aria-current', 'true');
    expect(projectIndicator).not.toHaveAttribute('aria-current');

    await user.click(projectIndicator);

    expect(projectIndicator).toHaveAttribute('aria-current', 'true');
    expect(aboutMeIndicator).not.toHaveAttribute('aria-current');
    expect(screen.getByText('About the Project')).toBeInTheDocument();
  });
});
