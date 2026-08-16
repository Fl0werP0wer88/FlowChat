import { ArrowLeft, ArrowRight } from 'lucide-react';
import { useState, type KeyboardEvent } from 'react';

const slides = [
  {
    id: 'about-me',
    eyebrow: 'About Me',
    title: 'I turn complex ideas into clear digital experiences.',
    description:
      "I'm Piotr Kwiatkowski, the designer and developer behind FlowChat, focused on pairing thoughtful interfaces with dependable engineering.",
  },
  {
    id: 'about-project',
    eyebrow: 'About the Project',
    title: 'FlowChat makes room for conversations that matter.',
    description:
      'FlowChat is a modern chat project built to keep communication focused, responsive, and ready to grow.',
  },
] as const;

type Direction = 'next' | 'previous';

export function BrandCarousel() {
  const [state, setState] = useState<{ direction: Direction; index: number }>({
    direction: 'next',
    index: 0,
  });
  const slide = slides[state.index] ?? slides[0];

  const move = (direction: Direction) => {
    setState((current) => ({
      direction,
      index:
        direction === 'next'
          ? (current.index + 1) % slides.length
          : (current.index - 1 + slides.length) % slides.length,
    }));
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      move('previous');
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      move('next');
    }
  };

  return (
    <section
      className="brand-copy brand-carousel"
      aria-label="About FlowChat"
      aria-roledescription="carousel"
    >
      <div className="brand-carousel__viewport" aria-live="polite" aria-atomic="true">
        <article
          key={`${slide.id}-${state.direction}`}
          className={`brand-carousel__slide brand-carousel__slide--${state.direction}`}
          role="group"
          aria-label={`${state.index + 1} of ${slides.length}`}
          aria-roledescription="slide"
        >
          <p className="brand-kicker">{slide.eyebrow}</p>
          <h2>{slide.title}</h2>
          <p className="brand-carousel__description">{slide.description}</p>
        </article>
      </div>

      <div className="brand-carousel__controls">
        <button
          className="brand-carousel__button"
          type="button"
          aria-label="Show previous section"
          onClick={() => move('previous')}
          onKeyDown={handleKeyDown}
        >
          <ArrowLeft aria-hidden="true" />
        </button>
        <span className="brand-carousel__counter" aria-hidden="true">
          {String(state.index + 1).padStart(2, '0')} / {String(slides.length).padStart(2, '0')}
        </span>
        <button
          className="brand-carousel__button"
          type="button"
          aria-label="Show next section"
          onClick={() => move('next')}
          onKeyDown={handleKeyDown}
        >
          <ArrowRight aria-hidden="true" />
        </button>
      </div>
    </section>
  );
}
