import { renderHook } from '@/testing/test-utils';

import { useChatContext } from '../use-chat-context';

describe('useChatContext', () => {
  it('throws a clear error outside ChatContextProvider', () => {
    expect(() => renderHook(() => useChatContext())).toThrow(
      'useChatContext must be used within ChatContextProvider.',
    );
  });
});
