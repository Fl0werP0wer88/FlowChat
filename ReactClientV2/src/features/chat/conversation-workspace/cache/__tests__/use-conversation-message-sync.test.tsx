import { QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';

import { createTestQueryClient } from '@/testing/test-utils';

import {
  catchUpConversationMessages,
  type CatchUpConversationMessagesResponse,
} from '../../api/catch-up-conversation-messages';
import type { ConversationMessage } from '../../api/conversation-contracts';
import {
  createConversationMessageBuffer,
  mergeConversationMessage,
  type ConversationMessageBufferState,
} from '../conversation-message-buffer';
import {
  fetchCatchUpPageWithRetry,
  runConversationMessageSyncSingleFlight,
  useConversationMessageSync,
} from '../use-conversation-message-sync';

vi.mock('../../api/catch-up-conversation-messages', () => ({
  catchUpConversationMessages: vi.fn(),
}));

const catchUpMock = vi.mocked(catchUpConversationMessages);
const conversationId = '40c3cd3b-69d8-4af3-b1a7-f9174537fb97';
const senderUserId = '91f65d44-d175-45af-8839-d2d36e7d61f9';
const queryKey = ['conversation-workspace', 'group', conversationId] as const;

function createMessage(sequenceNum: number, idSuffix = sequenceNum): ConversationMessage {
  return {
    id: `00000000-0000-4000-8000-${idSuffix.toString().padStart(12, '0')}`,
    conversationId,
    senderUserId,
    text: `Message ${sequenceNum}`,
    sentAtUtc: `2026-08-24T10:00:${sequenceNum.toString().padStart(2, '0')}+00:00`,
    sequenceNum,
  };
}

function createState(): ConversationMessageBufferState {
  return createConversationMessageBuffer({
    messages: [createMessage(1), createMessage(2)],
    currentSequenceNum: 2,
  });
}

function createPage(
  items: ConversationMessage[],
  options: Partial<CatchUpConversationMessagesResponse> = {},
): CatchUpConversationMessagesResponse {
  return {
    items,
    nextAfterSequenceNum: null,
    currentSequenceNum: 4,
    throughSequenceNum: 4,
    hasMore: false,
    ...options,
  };
}

function createSyncQueryClient() {
  const queryClient = createTestQueryClient();
  queryClient.setQueryDefaults(queryKey, { gcTime: Number.POSITIVE_INFINITY });
  return queryClient;
}

function createWrapper(queryClient: ReturnType<typeof createSyncQueryClient>) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('useConversationMessageSync', () => {
  beforeEach(() => {
    catchUpMock.mockReset();
    vi.useRealTimers();
  });

  it('loads multiple pages with one fixed snapshot and finalizes the cache', async () => {
    catchUpMock
      .mockResolvedValueOnce(
        createPage([createMessage(3)], {
          nextAfterSequenceNum: 3,
          hasMore: true,
        }),
      )
      .mockResolvedValueOnce(createPage([createMessage(4)]));
    const queryClient = createSyncQueryClient();
    queryClient.setQueryData(queryKey, createState());

    renderHook(() => useConversationMessageSync({ conversationId, queryKey, enabled: true }), {
      wrapper: createWrapper(queryClient),
    });

    await waitFor(() => expect(catchUpMock).toHaveBeenCalledTimes(2));
    await waitFor(() =>
      expect(queryClient.getQueryData<ConversationMessageBufferState>(queryKey)).toMatchObject({
        lastContiguousSequenceNum: 4,
        pendingMessagesBySequence: {},
        syncStatus: 'idle',
      }),
    );
    expect(catchUpMock.mock.calls[0]?.[0]).toMatchObject({
      afterSequenceNum: 2,
      throughSequenceNum: undefined,
      limit: 100,
    });
    expect(catchUpMock.mock.calls[1]?.[0]).toMatchObject({
      afterSequenceNum: 3,
      throughSequenceNum: 4,
      limit: 100,
    });
  });

  it('retries a catch-up page after 250, 500 and 1000 milliseconds', async () => {
    vi.useFakeTimers();
    catchUpMock
      .mockRejectedValueOnce(new Error('first'))
      .mockRejectedValueOnce(new Error('second'))
      .mockRejectedValueOnce(new Error('third'))
      .mockResolvedValueOnce(createPage([]));

    const request = fetchCatchUpPageWithRetry({
      conversationId,
      afterSequenceNum: 2,
    });
    await vi.advanceTimersByTimeAsync(1_750);

    await expect(request).resolves.toMatchObject({ throughSequenceNum: 4 });
    expect(catchUpMock).toHaveBeenCalledTimes(4);
  });

  it('sets an error status after all catch-up attempts fail', async () => {
    catchUpMock.mockRejectedValue(new Error('Unavailable'));
    const queryClient = createSyncQueryClient();
    queryClient.setQueryData(queryKey, createState());

    renderHook(() => useConversationMessageSync({ conversationId, queryKey, enabled: true }), {
      wrapper: createWrapper(queryClient),
    });

    await waitFor(
      () =>
        expect(queryClient.getQueryData<ConversationMessageBufferState>(queryKey)?.syncStatus).toBe(
          'error',
        ),
      { timeout: 3_000 },
    );
    expect(catchUpMock).toHaveBeenCalledTimes(4);
  });

  it('shares one active synchronization per conversation', async () => {
    let finish: ((value: number) => void) | undefined;
    const operation = vi.fn(
      () =>
        new Promise<number>((resolve) => {
          finish = resolve;
        }),
    );

    const first = runConversationMessageSyncSingleFlight(conversationId, operation);
    const second = runConversationMessageSyncSingleFlight(conversationId, operation);

    expect(first).toBe(second);
    expect(operation).toHaveBeenCalledOnce();
    finish?.(7);
    await expect(first).resolves.toBe(7);
  });

  it('invalidates the exact open query when catch-up detects a sequence conflict', async () => {
    catchUpMock.mockResolvedValue(createPage([createMessage(2, 99)], { throughSequenceNum: 2 }));
    const queryClient = createSyncQueryClient();
    queryClient.setQueryData(queryKey, createState());
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');

    renderHook(() => useConversationMessageSync({ conversationId, queryKey, enabled: true }), {
      wrapper: createWrapper(queryClient),
    });

    await waitFor(() => expect(invalidateQueries).toHaveBeenCalledWith({ queryKey, exact: true }));
  });

  it('starts another snapshot for a realtime message received during catch-up', async () => {
    let finishFirstPage: ((page: CatchUpConversationMessagesResponse) => void) | undefined;
    catchUpMock
      .mockImplementationOnce(
        () =>
          new Promise<CatchUpConversationMessagesResponse>((resolve) => {
            finishFirstPage = resolve;
          }),
      )
      .mockResolvedValueOnce(
        createPage([createMessage(5)], {
          currentSequenceNum: 6,
          throughSequenceNum: 6,
        }),
      );
    const queryClient = createSyncQueryClient();
    queryClient.setQueryData(queryKey, createState());

    renderHook(() => useConversationMessageSync({ conversationId, queryKey, enabled: true }), {
      wrapper: createWrapper(queryClient),
    });
    await waitFor(() => expect(catchUpMock).toHaveBeenCalledOnce());

    act(() => {
      queryClient.setQueryData<ConversationMessageBufferState>(queryKey, (current) =>
        current ? mergeConversationMessage(current, createMessage(6)).state : current,
      );
      finishFirstPage?.(createPage([createMessage(3), createMessage(4)]));
    });

    await waitFor(() => expect(catchUpMock).toHaveBeenCalledTimes(2));
    await waitFor(() =>
      expect(queryClient.getQueryData<ConversationMessageBufferState>(queryKey)).toMatchObject({
        lastContiguousSequenceNum: 6,
        pendingMessagesBySequence: {},
        syncStatus: 'idle',
      }),
    );
    expect(catchUpMock.mock.calls[1]?.[0]).toMatchObject({
      afterSequenceNum: 4,
      throughSequenceNum: undefined,
    });
  });
});
