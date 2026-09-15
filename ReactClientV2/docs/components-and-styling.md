# Components and styling

## Component ownership

Keep a component as close as possible to the behavior that owns it:

- feature-specific UI belongs in `src/features/<feature>/components`;
- shared primitives belong in `src/components/ui` only after a repeated need is established;
- shared application layouts and fallbacks belong in the corresponding `src/components` category;
- route modules should compose screens and routing concerns rather than absorb feature logic.

Extract a separate component when a section has a clear responsibility or complex JSX. Avoid nested render functions and components that accept many unrelated props. Prefer composition through children or small focused components.

Third-party UI behavior should be wrapped behind a project component when the wrapper adds a stable FlowChat API, styling, or accessibility contract.

## Accessibility

Critical flows must work with keyboard and assistive technology.

- Use semantic elements before adding ARIA.
- Every input has a visible label.
- Validation messages are associated with their controls.
- Invalid controls expose `aria-invalid`.
- Submission errors receive predictable focus when action is required.
- Buttons have clear accessible names and disabled/busy behavior.
- Status transitions are announced without repeatedly interrupting the user.
- Color is never the only error or success signal.

Automated checks support accessibility but do not replace keyboard and responsive review of critical screens.

## Visual language

The current FlowChat direction is calm, bright, and focused:

- bright content surfaces;
- deep navy brand planes;
- one cobalt interaction accent;
- strong brand typography;
- an abstract conversation-flow motif;
- limited chrome around meaningful actions.

Preserve this direction when adding authentication-adjacent UI. Future product areas may extend it through shared tokens rather than introducing unrelated palettes or decorative systems.

## Styling

Tailwind CSS 4 is the primary styling tool. Keep repeated class combinations behind an existing variant or component only when the repetition represents a stable visual contract. Use `cn` for intentional conditional composition.

Do not add a general component library merely to solve one local control. Prefer native semantic controls or focused headless primitives when their accessibility behavior would otherwise be expensive to implement correctly.

## Responsive behavior

- Design from narrow screens upward.
- Keep forms readable and avoid fixed dimensions that clip translated or validation content.
- Maintain comfortable touch targets.
- Ensure the brand plane supports the workflow instead of displacing the primary action on small screens.
- Test critical routes at mobile and desktop widths.

## Motion

Motion should explain hierarchy or state:

- restrained screen entrance;
- subtle field and button feedback;
- verification-status transitions;
- lightweight conversation-flow movement.

All non-essential animation must be disabled or reduced under `prefers-reduced-motion`. Avoid continuous motion that competes with form completion.

## Performance

- Lazy-load route modules.
- Keep state close to its consumers to limit re-renders.
- Avoid premature memoization; optimize measured work.
- Use build-time Tailwind styling rather than runtime CSS generation.
- Prefer composition when it naturally isolates frequently updating state.
- Lazy-load future non-critical images and use appropriate dimensions and modern formats.
- Watch production chunk sizes and add deliberate vendor splitting only when it improves cache or loading behavior.
